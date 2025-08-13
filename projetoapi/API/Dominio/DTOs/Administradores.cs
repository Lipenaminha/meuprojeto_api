namespace projetoapi.Dominio.DTOs;

using System;
using projetoapi.Dominio.Enuns;

public class AdministradoresDTO
{
    public string Email { get; set; } = default!;
    public string Senha { get; set; } = default!;

    public Perfil? Perfil { get; set; } = default!;

     public string Nome { get; set; } = default!;
}

