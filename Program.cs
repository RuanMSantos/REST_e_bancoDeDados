using REST_e_bancoDeDados.db;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TarefasContext>(opt => { // aqui é criada as configurações de conexão
// é passado um objeto de retorna uma função com todas as configurações listadas
    string connectionString = builder.Configuration.GetConnectionString("tarefasConnection");
    // cria uma variavel que contem a string de conexão, essa é a forma correta de fazer, sem colocar diretamente a string

    var serverVersion = ServerVersion.AutoDetect(connectionString); 
    // esse comando pega a string de conexão, conecta no banco e retorna sua versão

    opt.UseMySql(connectionString, serverVersion); // comando que cria efetivamente a conexão com o banco
    // são passados dois parâmetros, a string de conexão e a versão do banco
});

builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Endpoints da API

/*
app.MapGet("/", ([FromServices] TarefasContext _db) => { //  como esse comando será usado várias vezes,
    // podemos passá-lo como parametro, onde, ele vem dos serviços e é do tipo TarefasContext
    var tarefas = _db.Tarefa.ToList<Tarefa>();
    return Results.Ok(tarefas);
});*/

// METODO GET - é utilizado para trazer recursos do servidor para quem está chamando

// 1 - Busca por id
app.MapGet("/api/tarefas/{id}", ([FromServices] TarefasContext _db, // usa-se chaves para indicar que id é variavel 
    // como se trata de várias informações, coloca-se
    // no plural, regra do REST
    [FromRoute] int id // FromRoute pois a variavel esta vem da rota 
    ) => {
        var tarefa = _db.Tarefa.Find(id);

        if (tarefa == null){
            return Results.NotFound(); // retorna não encontrado, obviamente vazio
        }
        return Results.Ok(tarefa); // retorna a tarefa
        // usar os Results é uma recomendação do REST, por isso, deve ser seguida
});
/*
// 2 - Listagem completa
app.MapGet("/api/tarefas", ([FromServices] TarefasContext _db) => {
    // var tarefas = _db.Tarefa.ToList<Tarefa>();

    var query = _db.Tarefa.AsQueryable<Tarefa>();

    var tarefas = query.ToList<Tarefa>();
    return Results.Ok(tarefas);
});

// 3 - Filtro por descrição
app.MapGet("/api/tarefas", ([FromServices] TarefasContext _db,
    [FromQuery] string? descricao // quando adicionado uma ? na variavel significa que ela é opicional, pode existir ou não
    // entretando, mesmo se não existir o comando funciona normalmente. 
) => {
    var query = _db.Tarefa.AsQueryable<Tarefa>(); // este comando fala que query é um objeto consultavel
    // com isso pode-se trabalhar com varios filtros em cima dela

    if (!String.IsNullOrEmpty(descricao)){
        query = query.Where(t => t.Descricao.Contains(descricao)); // esse comando vai adicionar um filtro
        // dentro do query, note que ele não faz a consulta no banco. São realizados todos os comandos pra só após ir no banco 
    }

    var tarefas = query.ToList<Tarefa>(); // nessa situação nada mudou
    return Results.Ok(tarefas); // nesse caso, mesmo se estiver vazio, ele irá retornar, pois estar  vazio
    // é diferente de não conseguir retornar, apenas significa que o banco está vazio
});*/

// 4 - Filtro pela situação
app.MapGet("/api/tarefas", ([FromServices] TarefasContext _db,
    [FromQuery (Name = "Somente_pendentes")] bool? somentePendentes, // caso eu queria que o nome apareça diferente na query string é só passar o parametro name no from
    [FromQuery] string? descricao 
) => {
    bool filtrarPendentes = somentePendentes ?? false; // esse operador retorna um valor indicado caso a variavel seja nula, se não, ele retorna o valor da variavel

    var query = _db.Tarefa.AsQueryable<Tarefa>();

    if (!String.IsNullOrEmpty(descricao)){
        query = query.Where(t => t.Descricao.Contains(descricao));
    }

    if (filtrarPendentes){
        query = query.Where(t => !t.Concluida)
            .OrderByDescending(t => t.Id);
    } // note que essa condição pega a variavel query atual, que pode ja estar alterada

    var tarefas = query.ToList<Tarefa>(); 
    return Results.Ok(tarefas);
});

// METODO POST - é utilizado para enviar recursos para o servidor

// 5 - Inclusão
app.MapPost("/api/tarefas", ([FromServices] TarefasContext _db,
    [FromBody] Tarefa novaTarefa // como o body só pode receber uma informação, utiliza-se um objeto,
    // que nesse caso é do tipo tarefa para reaproveitar suas propriedades
    // no metodo post, utiliza-se o frombody, que possui varias vantagens como por exemplo criptografia dos dados em seu trafego
) => {
    if (String.IsNullOrEmpty(novaTarefa.Descricao)){
        return Results.BadRequest(new {
            mensagem = "Não é possível cadastrar tarefa sem descrição",
        }); // o REST recomenda usar BadRequest, e é uma boa prática retornar a mensagem em objeto
    }

    var tarefa = new Tarefa{
        Descricao = novaTarefa.Descricao,
        Concluida = novaTarefa.Concluida,
    }; // caso correto, é criado um objeto do tipo tarefa para conter as informações recebidas e enviar 
    // para o banco de dados

    _db.Tarefa.Add(tarefa);
    _db.SaveChanges();

    string urlTarefa = $"/api/tarefas/{tarefa.Id}"; // a url recebe o id da tarefa e com isso pode ser acessada
    // através do método get de consulta unica ja criado acima, onde recebe um id na url
    // note que quando as alterações são salvas no banco de dados, ele ja retorna atualizado agora contendo id

    return Results.Created(urlTarefa, tarefa); // o REST recomenda retornar o resultado juntamente com a URL
    // necessária para acessar esse resultado
    // com isso, o REST recomenda utilizar o status code Created, que receb dois parâmetros, a url e o resultado
});

// METODO PUT - esse metodo altera os dados antigos pelos novos a serem enviados, de forma que os antigos sejam substituídos

// 6 - Alteração
app.MapPut("/api/tarefas/{id}", ([FromServices] TarefasContext _db,
    [FromRoute] int id,
    [FromBody] Tarefa tarefaAtualizada
) => {
    if (tarefaAtualizada.Id != id){ // caso o usuario tenha alterado o id, o que é impossível
        return Results.BadRequest(new {
            mensagem = "Id inconsistente.",
        });
    }
    
    if (String.IsNullOrEmpty(tarefaAtualizada.Descricao)){
        return Results.BadRequest(new {
            mensagem = "Não é possível deixar uma tarefa sem descrição",
        });
    }

    var tarefa = _db.Tarefa.Find(id);

    if (tarefa == null){
        return Results.NotFound();
    }

    tarefa.Descricao = tarefaAtualizada.Descricao;
    tarefa.Concluida = tarefaAtualizada.Concluida;

    _db.SaveChanges();

    return Results.Ok(tarefa); // nesse caso não é necessário retornar a url, pois o usuario ja sabe a url, que é a mesma utilizada anteriormente para fazer a alteração
});

// METODO PATCH - é utilizado quando apenas alguns dados serão alterados

// 7 - Alteração (parcial)
app.MapPatch("/api/tarefas/{id}/concluir", ([FromServices] TarefasContext _db, // é comum nesse caso, colocar na rota qual ação de alteração será feita
    [FromRoute] int id
) => { 
    var tarefa = _db.Tarefa.Find(id);

    if (tarefa == null){
        return Results.NotFound();
    }

    if (tarefa.Concluida){
        return Results.BadRequest(new {
            mensagem = "Tarefa concluída anteriormente",
        });
    }

    tarefa.Concluida = true;
    _db.SaveChanges();

    return Results.Ok(tarefa);
}); 

// 8 - Exclusão
app.MapDelete("/api/tarefas/{id}", ([FromServices] TarefasContext _db,
    [FromRoute] int id
) => {
    var tarefa = _db.Tarefa.Find(id);

    if (tarefa == null){
        return Results.NotFound();
    }

    _db.Tarefa.Remove(tarefa);
    _db.SaveChanges();

    return Results.Ok(); // retorna-se o status vazio pois os dados foram deletados
});

app.Run();
