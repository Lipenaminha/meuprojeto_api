namespace projetoapi.Dominio.ModelViews;

public class ErrosDeValidacao
    {
        public List<string> Mensagens { get; set; }

        // Construtor sem parâmetros que inicializa a lista
        public ErrosDeValidacao()
        {
            Mensagens = new List<string>();
        }

        public ErrosDeValidacao(List<string> mensagens)
        {
            Mensagens = mensagens ?? new List<string>();
        }

        public ErrosDeValidacao AdicionarMensagem(string mensagem)
        {
            var novaLista = new List<string>(Mensagens) { mensagem };
            return new ErrosDeValidacao(novaLista);
        }

        // Implementação simples e segura
        public bool PossuiErros => Mensagens != null && Mensagens.Count > 0;
    }