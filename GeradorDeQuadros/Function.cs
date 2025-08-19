using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon.Lambda.Core;
using System.Buffers.Text;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace GeradorDeQuadros;

public class Function
{
    private readonly AmazonBedrockRuntimeClient _bedrock;
    
    public Function()
    {
        _bedrock = new AmazonBedrockRuntimeClient();
    }

    public async Task<QuadroGeradoResponse> FunctionHandler(GerarQuadroRequest request, ILambdaContext context)
    {
        const string basePrompt = "Quadro artístico no estilo {0} com o tema {1} Cores principais: {2}";
        const string model = "amazon.titan-image-generator-v1";
        const string contentType = "application/json";

        try
        {
            string traceId = Guid.NewGuid().ToString();

            context.Logger.LogInformation($"[Gerador de quadro] - {traceId} - Iniciando processamento para solicitação de geração de imagem");

            var cores = request.CoresPrincipais.Length > 0
                ? string.Join(", ", request.CoresPrincipais)
                : "nenhuma cor principal especificada";

            string prompt = string.Format(basePrompt, request.Estilo, request.Tema, cores);

            var modelParameters = new
            {
                taskType = "TEXT_IMAGE",
                textToImageParams = new
                {
                    text = $"Você é um especialista em arte e cria quadros exclusivos, crie um quadro com a seguinte especificação: {prompt}"
                },
                imageGenerationConfig = new 
                {
                    numberOfImages = 1,
                    height = 1024,
                    width = 1024,
                    cfgScale = 7.5
                }
        };

            using var body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(modelParameters)));

            var invokeModelRequest = new InvokeModelRequest
            {
                ModelId = model,
                Body = body,
                ContentType = contentType,
                Accept = contentType,
            };

            var invokeModelResponse = await _bedrock.InvokeModelAsync(invokeModelRequest);


            if (invokeModelResponse.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                context.Logger.LogError($"[Gerador de quadro] - {traceId} - Erro ao invocar modelo: {invokeModelResponse.HttpStatusCode}");
                throw new Exception("Erro ao invocar modelo de geração de imagem");
            }

            context.Logger.LogInformation($"[Gerador de quadro] - {traceId} - Imagem gerada com sucesso, tamanho: {invokeModelResponse.ContentLength} bytes");

            var responseBody = await JsonNode.ParseAsync(invokeModelResponse.Body);

            context.Logger.LogInformation($"[Gerador de quadro] - {traceId} - Processamento finalizado");

            string imagemGerada = responseBody?["images"]?[0]?.ToString() ?? string.Empty;

            return new QuadroGeradoResponse()
            {
                Sucesso = !string.IsNullOrWhiteSpace(imagemGerada),
                Base64Imagem = imagemGerada
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex, $"[Gerador de quadro] - Erro ao processar solicitação");

            return new QuadroGeradoResponse()
            {
                Sucesso = false,
                Base64Imagem = string.Empty
            };
        }
    }
}
