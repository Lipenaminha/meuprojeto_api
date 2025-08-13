





using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using projetoapi.Dominio.DTOs;
using projetoapi.Dominio.Entidades;
using projetoapi.Dominio.Enuns;
using projetoapi.Dominio.Interfaces;
using projetoapi.Dominio.ModelViews;
using projetoapi.Dominio.Servicos;
using projetoapi.Infraestrutura.Db;




#region Builder
var builder = WebApplication.CreateBuilder(args);

var key = builder.Configuration.GetSection("Jwt").ToString();
if (string.IsNullOrEmpty(key)) key = "12345678";

builder.Services.AddAuthentication(option =>
{
    option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(option =>
{
    option.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),

        ValidateIssuer = false,
        ValidateAudience = false,
    };
});

builder.Services.AddAuthorization();

builder.Services.AddScoped<IAdministradorServico, AdministradorServico>();
builder.Services.AddScoped<IVeiculoServico, VeiculoServico>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o token JWT Aqui"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme{
              Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
              },
              new string[] {}
        },
    });
});

builder.Services.AddDbContext<DbContexto>(options =>
{
    options.UseMySql(
        builder.Configuration.GetConnectionString("mysql"), // sua connection string
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("mysql"))
    );
});


builder.Services.AddDbContext<DbContexto>(options =>
{
    options.UseMySql(
        builder.Configuration.GetConnectionString("mysql"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("mysql")));
});

var app = builder.Build();
#endregion

#region Home
app.MapGet("/", () => Results.Json(new Home())).AllowAnonymous().WithTags("Home");
#endregion

#region Administradores
string GerarTokenJwt(Administrador administrador)
{

    if (string.IsNullOrEmpty(key)) return string.Empty;

    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>()
    {
        new Claim("Email", administrador.Email),
        new Claim("Perfil", administrador.Perfil),
        new Claim(ClaimTypes.Role, administrador.Perfil),
    };

    var token = new JwtSecurityToken(
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}
app.MapPost("/administradores/login", ([FromBody] LoginDTO loginDTO, IAdministradorServico administradorServico) =>
{
    var adm = administradorServico.Login(loginDTO);
    if (adm != null)
    {


        string token = GerarTokenJwt(adm);
        return Results.Ok(new AdministradorLogado
        {
            Email = adm.Email,
            Perfil = adm.Perfil,
            Token = token
        });
    } else
    {
        return Results.Unauthorized();
    }

    


   /* if (administradorServico.Login(loginDTO) != null)
         return Results.Ok("Login com sucesso!");
     else
         return Results.Unauthorized();
         */

}).AllowAnonymous().WithTags("Administradores");

app.MapGet("/administradores", ([FromQuery] int? pagina, IAdministradorServico administradorServico) =>
{
    var adms = new List<AdministradorModelView>();
    var administradores = administradorServico.Todos(pagina);
    foreach (var adm in administradores)
    {
        adms.Add(new AdministradorModelView
        {
            Id = adm.Id,
            Email = adm.Email,
            Perfil = adm.Perfil.ToString()
        });


    }
    return Results.Ok(adms);
}).RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute { Roles = "Adm" })
.WithTags("Administradores");

app.MapGet("/administradores/{id}", ([FromRoute] int id, IAdministradorServico administradorServico) =>
{
    var administrador = administradorServico.BuscaPorId(id);
    if (administrador == null) return Results.NotFound();
    return Results.Ok(new AdministradorModelView
        {
            Id = administrador.Id,
            Email = administrador.Email,
            Perfil = administrador.Perfil.ToString()
        });
}).RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute { Roles = "Adm" })
.WithTags("Administradores");

app.MapPost("/administradores", ([FromBody] AdministradoresDTO administradorDTO, IAdministradorServico administradorServico) =>
{
    var validacao = new ErrosDeValidacao();
    var mensagens = new List<string>();

    if (string.IsNullOrEmpty(administradorDTO.Email))
        mensagens.Add("O email é obrigatório.");

    if (string.IsNullOrEmpty(administradorDTO.Senha))
        mensagens.Add("A senha é obrigatória.");

    if (mensagens.Any())
    {
        validacao.AdicionarErro(mensagens);
        return Results.BadRequest(validacao);
    }

    var administrador = new Administrador
    {
        Email = administradorDTO.Email,
        Perfil = administradorDTO.Perfil?.ToString() ?? Perfil.Editor.ToString()
    };

    var novoAdministrador = administradorServico.Incluir(administrador);


    return Results.Created($"/administradores/{novoAdministrador.Id}", novoAdministrador);

})
.RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute { Roles = "Adm" })
.WithTags("Administradores");
#endregion

#region Veiculos

ErrosDeValidacao validaDTO(VeiculoDTO veiculoDTO)
{
    var validacao = new ErrosDeValidacao();

    if (string.IsNullOrEmpty(veiculoDTO.Nome))
        validacao = validacao.AdicionarMensagem("O nome do veículo é obrigatório.");

    if (string.IsNullOrEmpty(veiculoDTO.Marca))
        validacao = validacao.AdicionarMensagem("A marca do veículo é obrigatória.");

    if (veiculoDTO.Ano < 1950)
        validacao = validacao.AdicionarMensagem("O ano do veículo deve ser maior ou igual a 1950.");

    return validacao;
}

app.MapPost("/veiculos", ([FromBody] VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>
{
    var validacao = validaDTO(veiculoDTO);

    if (validacao.PossuiErros)
        return Results.BadRequest(validacao);

    var veiculo = new Veiculo
    {
        Nome = veiculoDTO.Nome,
        Marca = veiculoDTO.Marca,
        Ano = veiculoDTO.Ano
    };

    veiculoServico.Incluir(veiculo);

    return Results.Created($"/veiculos/{veiculo.Id}", veiculo);

})
.RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute { Roles = "Adm, Editor" })
.WithTags("Veículos");

app.MapGet("/veiculos", (int? pagina, IVeiculoServico veiculoServico) =>
{
    var veiculos = veiculoServico.Todos(pagina);

    return Results.Ok(veiculos);
}).WithTags("Veículos");

app.MapGet("/veiculos/{id}", (int id, IVeiculoServico veiculoServico) =>
{
    var veiculo = veiculoServico.BuscaPorId(id);

    return veiculo != null ? Results.Ok(veiculo) : Results.NotFound();
})
.RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute { Roles = "Adm" })
.WithTags("Veículos");

app.MapPut("/veiculos/{id}", (int id, [FromBody] VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>
{
    var veiculo = veiculoServico.BuscaPorId(id);
    if (veiculo == null) return Results.NotFound();

    var validacao = validaDTO(veiculoDTO);
    if (validacao.PossuiErros) return Results.BadRequest(validacao);

    veiculo.Nome = veiculoDTO.Nome;
    veiculo.Marca = veiculoDTO.Marca;
    veiculo.Ano = veiculoDTO.Ano;

    veiculoServico.Atualizar(veiculo);
    return Results.Ok(veiculo);

}).RequireAuthorization().WithTags("Veículos");

app.MapDelete("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico) =>
{
    var veiculo = veiculoServico.BuscaPorId(id);
    if (veiculo == null) return Results.NotFound();

    veiculoServico.Apagar(veiculo);

    return Results.NoContent();
}).RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute { Roles = "Adm"})
.WithTags("Veículos");

#endregion

#region App
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.Run();

internal class VeiculoModelView
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string Marca { get; set; }
    public int Ano { get; set; }
}

internal class ErrosDeValidacao
{
    private List<string> _mensagens = new List<string>();

    public IEnumerable<string> Mensagens => _mensagens;

    public bool TemErros => _mensagens.Count > 0;

    public bool PossuiErros { get; private set; }

    internal void AdicionarErro(List<string> mensagens)
    {
        _mensagens.AddRange(mensagens);
        PossuiErros = true;
    }

    internal ErrosDeValidacao AdicionarMensagem(string v)
    {
        _mensagens.Add(v);
        return this;
    }
}
#endregion
/*

#region Builder
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IAdministradorServico, AdministradorServico>();

builder.Services.AddScoped<IVeiculoServico, VeiculoServico>();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<DbContexto>(options =>
{

    options.UseMySql(
        builder.Configuration.GetConnectionString("mysql"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("mysql")));
});


var app = builder.Build();
#endregion



#region Home
app.MapGet("/", () => Results.Json(new Home())).WithTags("Home");
#endregion

#region Administradores
app.MapPost("/administradores/login", ([FromBody] LoginDTO loginDTO, IAdministradorServico administradorServico) =>
{
    if (administradorServico.Login(loginDTO) != null)
        return Results.Ok("Login com sucesso!");
    else
        return Results.Unauthorized();

}).WithTags("Administradores");

app.MapGet("/administradores", ([FromQuery] int? pagina, IAdministradorServico administradorServico) =>
{
    return Results.Ok(administradorServico.Todos(pagina));

}).WithTags("Administradores");

app.MapGet("/administradores/{id}", ([FromRoute]int id, IAdministradorServico administradorServico) =>
{
    var administrador = administradorServico.BuscaPorId(id);
    if (administrador == null) return Results.NotFound();
    return Results.Ok(administrador);
}).WithTags("Administradores");


//..

app.MapPost("/administradores", ([FromBody] AdministradoresDTO administradorDTO, IAdministradorServico administradorServico) =>
{
    var validacao = new ErrosDeValidacao();
    var Mensagens = new List<string>();


    if (string.IsNullOrEmpty(administradorDTO.Email))
        Mensagens.Add("O email é obrigatório.");

    if (string.IsNullOrEmpty(administradorDTO.Senha))
        Mensagens.Add("A senha é obrigatória.");

    if (Mensagens.Any())
    {
        validacao.AdicionarErro(Mensagens);
        return Results.BadRequest(validacao);
    }


    var administrador = new Administrador
    {
        Email = administradorDTO.Email,
        Senha = administradorDTO.Senha,
        Perfil = administradorDTO.Perfil.ToString() ?? Perfil.Editor.ToString()
    };

    var novoAdministrador = administradorServico.Incluir(administrador);

    return Results.Created($"/administradores/{novoAdministrador.Id}", novoAdministrador);

}).WithTags("Administradores");
#endregion



#region Veiculos


ErrosDeValidacao validaDTO(VeiculoDTO veiculoDTO)
{
     // a mensagem não está aparecendo no swagger , resolver o problema depois.
    var validacao = new ErrosDeValidacao();

    validacao = validacao.AdicionarMensagem("O nome é obrigatório");


    if (string.IsNullOrEmpty(veiculoDTO.Nome))

        validacao = validacao.AdicionarMensagem("O nome do veículo é obrigatório.");


    if (string.IsNullOrEmpty(veiculoDTO.Marca))

        validacao = validacao.AdicionarMensagem("A marca do veículo é obrigatória.");


    if (veiculoDTO.Ano < 1950)

        validacao = validacao.AdicionarMensagem("O ano do veículo deve ser maior ou igual a 1950.");

    return validacao;


}
app.MapPost("/veiculos", ([FromBody] VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>
{
    // a mensagem não está aparecendo no swagger , resolver o problema depois.
  var validacao = validaDTO(veiculoDTO);

    if (validacao.PossuiErros)
        return Results.BadRequest(validacao);  // Retorna erro se houver

    var veiculo = new Veiculo
    {
        Nome = veiculoDTO.Nome,
        Marca = veiculoDTO.Marca,
        Ano = veiculoDTO.Ano
    };

    veiculoServico.Incluir(veiculo);

    return Results.Created($"/veiculos/{veiculo.Id}", veiculo);  // Retorna 201 Created

}).WithTags("Veículos");

app.MapGet("/veiculos", (int? pagina, IVeiculoServico veiculoServico) =>
{
    var veiculos = veiculoServico.Todos(pagina);

    return Results.Ok(veiculos);
}).WithTags("Veículos");

app.MapGet("/administradores", ([FromQuery] int? pagina, IAdministradorServico administradorServico) =>
{
    return Results.Ok(administradorServico.Todos(pagina));

}).WithTags("Administradores");

/*app.MapGet("/administradores", ([FromQuery] int? pagina, IAdministradorServico administradorServico) =>
{
    return Results.Ok(administradorServico.Todos(pagina));

}).WithTags("Administradores");
*/

/*

app.MapGet("/Administradores/{id}", (int id, IAdministradorServico administradorServico) =>
{
    var administrador = administradorServico.BuscaPorId(id);

    return administrador != null ? Results.Ok(administrador) : Results.NotFound();
});
*/

/* app.MapGet("/veiculos/{id}", (int id, IVeiculoServico veiculoServico) =>
{
    var veiculo = veiculoServico.BuscaPorId(id);

    return veiculo != null ? Results.Ok(veiculo) : Results.NotFound();
}).WithTags("Veículos");

*/

/*
app.MapPut("/veiculos/{id}", (int id, [FromBody] VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>
{
    var veiculo = veiculoServico.BuscaPorId(id);
    if (veiculo == null) return Results.NotFound();

     var validacao = validaDTO(veiculoDTO);
    if (validacao.PossuiErros) return Results.BadRequest(validacao);


    veiculo.Nome = veiculoDTO.Nome;
    veiculo.Marca = veiculoDTO.Marca;
    veiculo.Ano = veiculoDTO.Ano;

    veiculoServico.Atualizar(veiculo);
    return Results.Ok(veiculo);

}).WithTags("Veículos");

app.MapDelete("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico) =>
{
    var veiculo = veiculoServico.BuscaPorId(id);
    if (veiculo == null) return Results.NotFound();

    veiculoServico.Apagar(veiculo);

    return Results.NoContent();
}).WithTags("Veículos");



#endregion

#region App
app.UseSwaggerUI();
app.UseSwagger();

app.Run();

internal class ErrosDeValidacao
{
    private List<string> _mensagens = new List<string>();

    public ErrosDeValidacao()
    {
    }

    public IEnumerable<string> Mensagens => _mensagens;

    public bool TemErros => _mensagens.Count > 0;

    public bool PossuiErros { get; internal set; }

    internal void AdicionarErro(List<string> mensagens)
    {
        throw new NotImplementedException();
    }

    internal ErrosDeValidacao AdicionarMensagem(string v)
    {
        _mensagens.Add(v);
        return this;
    }
}
#endregion
*/