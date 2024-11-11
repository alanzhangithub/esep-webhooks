using System.Text.Json;
using System.Text.Json.Nodes;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using System.Net.Http;
using System.Text;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook;

public class Function
{
    private static readonly HttpClient client = new HttpClient();
    
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            context.Logger.LogInformation("Received request: " + JsonSerializer.Serialize(request));

            // Parser stuff
            var githubEvent = JsonNode.Parse(request.Body);
            var issueUrl = githubEvent["issue"]?["html_url"]?.ToString();
            
            if (string.IsNullOrEmpty(issueUrl))
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = 400,
                    Body = JsonSerializer.Serialize(new { message = "Not a valid issue event" })
                };
            }

            //get slack url webhook
            var slackUrl = Environment.GetEnvironmentVariable("SLACK_URL");
            if (string.IsNullOrEmpty(slackUrl))
            {
                throw new Exception("SLACK_URL environment variable is not set");
            }

            
            var slackMessage = new
            {
                text = $"New Issue Created: {issueUrl}"
            };

            
            var content = new StringContent(
                JsonSerializer.Serialize(slackMessage),
                Encoding.UTF8,
                "application/json");
                
            await client.PostAsync(slackUrl, content);

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize(new { message = "it works" }),
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" }
                }
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error processing webhook: {ex.Message}");
            return new APIGatewayProxyResponse
            {
                StatusCode = 500,
                Body = JsonSerializer.Serialize(new { message = "bad" })
            };
        }
    }
}