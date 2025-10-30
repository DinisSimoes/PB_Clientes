namespace PB_Clientes.Domain.Entities
{
    public class Cliente
    {
        public Guid Id { get; private set; }
        public string Nome { get; private set; } = string.Empty;
        public string Cpf { get; private set; } = string.Empty;
        public DateTime DataCriacao { get; private set; }

        private Cliente() { }

        public Cliente(string nome, string cpf)
        {
            Id = Guid.NewGuid();
            Nome = nome;
            Cpf = cpf;
            DataCriacao = DateTime.UtcNow;
        }
    }
}
