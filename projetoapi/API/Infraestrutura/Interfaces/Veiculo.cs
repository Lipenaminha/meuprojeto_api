
using projetoapi.Dominio.Entidades;
using projetoapi.Dominio.Interfaces;
using projetoapi.Dominio.DTOs;
using projetoapi.Infraestrutura.Db;



public interface IVeiculoServico
{

    List<Veiculo> Todos(int? pagina = 1, string? nome = null, string? marca = null);

    Veiculo? BuscaPorId(int id);

    void Incluir(Veiculo Veiculo);

    void Atualizar(Veiculo Veiculo);

    void Apagar(Veiculo Veiculo);
 
}

