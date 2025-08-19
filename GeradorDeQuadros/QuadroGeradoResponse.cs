namespace GeradorDeQuadros
{
    public record QuadroGeradoResponse
    {
        public required string Base64Imagem { get; init; }
        public required bool Sucesso { get; init; }
    }
}
