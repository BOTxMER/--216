List<Order> repo = [
    new(1,new(2024,4,20),"принтер","LG P.DIDDY 200","Не работает","Иванов Иван Иванович","88005553535","новая заявка"),
    new(2,new(2024,6,21),"лаптом","Ardor","Пролил кока колу","Максимов Максим Максимович","89999999999","новая заявка")
];

List<string> notifications = new();

var builder = WebApplication.CreateBuilder();
builder.Services.AddCors();
var app = builder.Build();

app.UseCors(option => option
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

string message = "";

app.MapGet("/orders", (int param = 0) =>
{
    string buffer = message;
    message = "";
    if (param != 0)
        return new { repo = repo.FindAll(x => x.Number == param), message = buffer };
    return new { repo, message = buffer };
});

app.MapPost("/create", async (HttpContext context) =>
{
    try
    {
        var order = await context.Request.ReadFromJsonAsync<Order>();
        if (order == null)
        {
            return Results.BadRequest("Неверные данные заявки");
        }
        repo.Add(order);
        return Results.Ok();
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: "Ошибка при создании заявки: " + ex.Message, statusCode: 500);
    }
});


app.MapGet("/update", ([AsParameters] UpdateOrderDTO dto) =>
{
    var order = repo.Find(x => x.Number == dto.Number);
    if (order == null)
        return;
    if (dto.Status != order.Status && dto.Status != "")
    {
        order.Status = dto.Status;
        message += $"Статус заявки №{order.Number} изменен\n";
        if (order.Status == "выполнено")
        {
            message += $"Заявка №{order.Number} завершена\n";
            order.EndDate = DateOnly.FromDateTime(DateTime.Now);
        }
    }
    if (dto.Description != "")
        order.Description = dto.Description;
    if (dto.Master != "")
        order.Master = dto.Master;
    if(dto.Comment !="")
        order.Comments.Add(dto.Comment);
});


int complete_count() => repo.FindAll(x => x.Status == "выполнено").Count;
Dictionary<string, int> get_problem_type_stat() =>
    repo.GroupBy(x => x.Description).Select(x => (x.Key, x.Count())).ToDictionary(k => k.Key, v => v.Item2);

double get_average_time_to_complete() =>
    complete_count() == 0 ? 0 :
    repo.FindAll(x => x.Status == "выполнено").Select(x => x.EndDate.Value.DayNumber - x.StartDate.DayNumber).Sum() / complete_count();

app.MapGet("/statistics", () => new
{
    complete_count = complete_count(),
    problem_type_stat = get_problem_type_stat(),
    average_time_to_complete = get_average_time_to_complete(),
});

app.Run();

class Order
{
    public Order(int number, DateOnly startDate, string orgtech, string model, string description, string fio, string phone, string status)
    {
        Number = number;
        StartDate = startDate;
        Orgtech = orgtech;
        Model = model;
        Description = description;
        Fio = fio;
        Phone = phone;
        Status = status;
    }

    public int Number { get; set; }
    public DateOnly StartDate { get; set; }
    public string Orgtech { get; set; }
    public string Model { get; set; }
    public string Description { get; set; }
    public string Fio { get; set; }
    public string Phone { get; set; }
    public string Status { get; set; }
    public DateOnly? EndDate { get; set; } = null;
    public string? Master { get; set; } = "Бомбастер";
    public List<string>? Comments { get; set;} = [];
}

record class UpdateOrderDTO(int Number, string? Status = "", string? Description = "", string? Master = "",string? Comment = "");
