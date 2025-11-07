using MassTransit;
using PaymentService.Api.Consumers;
using PaymentService.Api.Tracing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<TraceStore>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    // открытый CORS для локальной отладки
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddMassTransit(x =>
{
    // сервис принимает OrderCreated и публикует PaymentCompleted
    x.AddConsumer<OrderCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        // параметры подключения к RabbitMQ берутся из конфигурации
        cfg.Host(builder.Configuration.GetValue<string>("RabbitMq:Host") ?? "rabbitmq", "/", h =>
        {
            h.Username(builder.Configuration.GetValue<string>("RabbitMq:Username") ?? "guest");
            h.Password(builder.Configuration.GetValue<string>("RabbitMq:Password") ?? "guest");
        });

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();

app.Run();
