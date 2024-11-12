using System.Text.Json;
List<string> Notifications = new List<string>();
List<string> NotificationsBot = new List<string>();
Repository repository = new Repository();
List<Order> Orders = new List<Order>
{
    new Order("Телефон","Сломался экран","Много трещин","Максим"),
    new Order("Планшет","Лагает","Вмятины","Саня"),
    new Order("Компьютер","Не запускается","Неизвестно","Богдан")
};
repository.Orders = Orders;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("Open", builder => builder.WithOrigins("http://127.0.0.1:5500").AllowAnyHeader().AllowAnyMethod());
});
var app = builder.Build();
app.UseCors("Open");
app.MapGet("/", () =>
{
    var data = new
    {
        orders = repository.ReadAll(),
        notifications = Notifications.ToList(),
    };
    Console.WriteLine(Notifications);
    Notifications.Clear();
    return Results.Json(data);
});
app.MapGet("/bot", () =>
{
    var data = new
    {
        orders = repository.ReadAll(),
        notifications = NotificationsBot.ToList(),
    };
    NotificationsBot.Clear();
    return Results.Json(data);
});
app.MapGet("/order/id/{id}", (int id) => repository.Read(id));
app.MapGet("/statistics", () =>
{
    var statistics = new
    {
        CompletedOrders = repository.CompleteOrders(),
        AverageExecutionTime = repository.AverageExecutionTime(),
        ProblemTypeStatistics = repository.ProblemTypeStatictics()
    };
    return Results.Json(statistics);
});
app.MapPost("/order/add", (Order order) =>
{
    repository.AddOrder(new Order(order.Device, order.ProblemType, order.Description, order.Client));
    Notifications.Add($"Заявка добавлена");
    NotificationsBot.Add($"Заявка добавлена");
});
app.MapPut("/order/update/id/{id}", (int id, Order order) =>
{
    var orderOld = repository.Read(id);
    if (!string.IsNullOrEmpty(order.Device))
    {
        orderOld.Device = order.Device;
    }
    if (!string.IsNullOrEmpty(order.ProblemType))
    {
        orderOld.ProblemType = order.ProblemType;
    }
    if (!string.IsNullOrEmpty(order.Description))
    {
        orderOld.Description = order.Description;
    }
    if (!string.IsNullOrEmpty(order.Client))
    {
        orderOld.Client = order.Client;
    }
    if (Enum.IsDefined(typeof(Status), order.Status))
    {
        if (order.Status == Status.Complete)
        {
            Notifications.Add($"Заявка {id} выполнена");
            NotificationsBot.Add($"Заявка {id} выполнена");
        }
        orderOld.Status = order.Status;
        if (order.Status == Status.InProcess)
        {
            Notifications.Add($"Заявка {id} в работе");
            NotificationsBot.Add($"Заявка {id} работе");
        }
        orderOld.Status = order.Status;
    }
    if (!string.IsNullOrEmpty(order.Master))
    {
        orderOld.Master = order.Master;
    }
    if (!string.IsNullOrEmpty(order.Comment))
    {
        orderOld.Comment = order.Comment;
    }
    Notifications.Add($"Заявка {id} обновлена");
    NotificationsBot.Add($"Заявка {id} обновлена");
    if (order.StartDate != null)
    {
        orderOld.StartDate = order.StartDate;
    }
    if (order.EndDate != null)
    {
        orderOld.EndDate = order.EndDate;
    }
});
app.MapDelete("/order/delete/id/{id}", (int id) => repository.DeleteOrder(id));
app.Run();
public enum Status
{
    InWaiting, InProcess, Complete
}
class Order
{
    public int Id { get; set; }
    private DateTime? startDate;
    private DateTime? endDate;
    private string device;
    private string problemType;
    private string description;
    private string client;
    private Status status;
    public DateTime? StartDate
    {
        get => startDate;
        set
        {
            if (value.HasValue)
            {
                startDate = value;
            }
            else
            {
                throw new ArgumentException("Заполните дату");
            }
        }
    }
    public DateTime? EndDate
    {
        get => endDate;
        set
        {
            if (value.HasValue)
            {
                endDate = value;
            }
            else
            {
                throw new ArgumentException("Заполните дату");
            }
        }
    }
    public string Device { 
        get =>device;
        set 
        {
            if (!string.IsNullOrEmpty(value))
                device = value;
            else
                throw new ArgumentException("Заполните название устройства");
        }
    }
    public string ProblemType {
        get => problemType;
        set
        {
            if (!string.IsNullOrEmpty(value))
                problemType = value;
            else
                throw new ArgumentException("Заполните тип проблемы");
        } 
    }
    public string Description { 
        get => description; 
        set        
        {
            if (!string.IsNullOrEmpty(value))
            description = value;
        else
            throw new ArgumentException("Заполните описания");
        } 
    }
    public string Client { 
        get => client; 
        set
        {
            if (!string.IsNullOrEmpty(value))
                client = value;
            else
                throw new ArgumentException("Заполнитн имя клиента");
        }
    }
    public string Master { get; set; }
    public string Comment { get; set; }
    public Status Status
    {
        get => status;
        set
        {
            if (value == Status.Complete)
            {
                EndDate = DateTime.Now;
            }
            status = value;
            if (value == Status.InWaiting)
            {
                EndDate = DateTime.MinValue;
            }
            status = value;
        }
    }
    public Order() { } 
    public Order(string device, string problemType, string description, string client)
    {
        Id = IdChek++;
        StartDate = DateTime.Now;
        EndDate = DateTime.MinValue;
        Device = device;
        ProblemType = problemType;
        Description = description;
        Client = client;
        Master = "";
        Comment = "";
        Status = Status.InWaiting;
    }
    public static int IdChek { get; set; } = 1;
}
class Repository
{
    public List<Order> Orders { get; set; } = new List<Order>();
    public void AddOrder(Order order)
    {
        Orders.Add(order);
    }
    public Order Read(int id)
    {
        return Orders.ToList().Find(x => x.Id == id);
    }
    public List<Order> ReadAll()
    {
        return Orders.ToList();
    }
    public void DeleteOrder(int id)
    {
        Orders.Remove(Read(id));
    }
    public int CompleteOrders()
    {
        return Orders.Count(o => o.Status == Status.Complete);
    }
    public TimeSpan AverageExecutionTime()
    {
        var completeOrders = Orders.Where(o => o.Status == Status.Complete);
        if (completeOrders.Any())
        {
            return TimeSpan.FromSeconds((double)completeOrders.Average(o => (o.EndDate - o.StartDate)?.Seconds));
        }
        return TimeSpan.Zero;
    }
    public Dictionary<string, int> ProblemTypeStatictics()
    {
        return Orders.ToList()
           .GroupBy(o => o.ProblemType)
           .ToDictionary(g => g.Key, g => g.Count());
    }
}