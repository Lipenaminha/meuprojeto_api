
using projetoapi.Dominio.Entidades;
using projetoapi.Dominio.Interfaces;
using projetoapi.Dominio.DTOs;
using projetoapi.Infraestrutura.Db;

namespace projetoapi.Dominio.Interfaces
{

    public interface IAdministradorServico
    {

        Administrador? Login(LoginDTO loginDTO);

        Administrador? Incluir(Administrador administrador);

        Administrador?BuscaPorId(int id);

      List<Administrador> Todos(int? pagina);


}
}

