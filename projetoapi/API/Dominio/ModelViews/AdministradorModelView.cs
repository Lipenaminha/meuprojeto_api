using projetoapi.Dominio.Enuns;

namespace projetoapi.Dominio.ModelViews
{
    public record AdministradorModelView
    {

        public int Id { get; set; }

        public string Email { get; set; }


        public string Perfil { get; set; }
    }
}