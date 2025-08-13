using projetoapi.Dominio.DTOs;
using projetoapi.Dominio.Entidades;
using projetoapi.Dominio.Interfaces;
using projetoapi.Infraestrutura.Db;
using System.Linq;

namespace projetoapi.Dominio.Servicos
{
    public class AdministradorServico : IAdministradorServico
    {
        private readonly DbContexto _contexto;

        public AdministradorServico(DbContexto contexto)
        {
            _contexto = contexto;
        }

        public Administrador BuscaPorId(int id)
        {
#pragma warning disable CS8603 // Possible null reference return.
            return _contexto.Administradores.Where(v => v.Id == id).FirstOrDefault();
#pragma warning restore CS8603 // Possible null reference return.
        }

        public Administrador? Incluir(Administrador administrador)
        {
            _contexto.Administradores.Add(administrador);
            _contexto.SaveChanges();
            return administrador;
            

        }

        public Administrador? Login(LoginDTO loginDTO)
        {
            return _contexto.Administradores
                .FirstOrDefault(a => a.Email == loginDTO.Email && a.Senha == loginDTO.Senha);
        }

        public List<Administrador> Todos(int? pagina)
        {
            var query = _contexto.Administradores.AsQueryable();


        int itensPorPagina = 10;

        if (pagina != null)
            query = query.Skip(((int)pagina - 1) * itensPorPagina).Take(itensPorPagina);

        return [.. query];
        }
    }
}