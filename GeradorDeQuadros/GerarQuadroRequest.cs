namespace GeradorDeQuadros
{
    public record GerarQuadroRequest
    {
        public required string Estilo { get; init; }
        public required string Tema { get; init; }
        public required string[] CoresPrincipais { get; init; }
    }
}