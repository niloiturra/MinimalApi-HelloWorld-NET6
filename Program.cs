using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MinimalApiDemo.Data;
using MinimalApiDemo.Data.Auth;
using MinimalApiDemo.Entities;
using MinimalApiDemo.Models;
using MiniValidation;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

#region Configure Services

builder.Services.Configure<JwtAppSettings>(builder.Configuration.GetSection("JWT"));

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDbContext<MinimalContextDb>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddDbContext<AuthContextDb>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<AuthContextDb>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = false,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"])),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Minimal API Demo",
        Description = "An API demo developed for study purposes and \"hello world\", developed inspired by the explanations of the developer.io Youtube channel",
        Contact = new OpenApiContact { Name = "Nilo Alan" },
        License = new OpenApiLicense { Name = "MIT" }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter the JWT in this format: Bearer {token}",
        Name = "Authorization",
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[]{ }
        }
    });
});

var app = builder.Build();

#endregion

#region Configure Pipeline

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication().UseAuthorization();

MapActions(app);

app.Run();

#endregion

#region Endpoints

void MapActions(WebApplication app)
{
    app.MapGet("/products", [Authorize] async (MinimalContextDb context) =>
        await context.Products.ToListAsync())
    .WithName("GetProducts")
    .WithTags("Products");

    app.MapGet("/product/{id}", async (Guid id, MinimalContextDb context) =>
        await context.Products.FindAsync(id) is Product product
            ? Results.Ok(product)
            : Results.NotFound())
        .Produces<Product>(StatusCodes.Status200OK)
        .Produces<Product>(StatusCodes.Status404NotFound)
        .WithName("GetProductById")
        .WithTags("Products");

    app.MapPost("/product", [Authorize] async (
        MinimalContextDb context,
        Product product) =>
    {
        if (!MiniValidator.TryValidate(product, out var errors))
            return Results.ValidationProblem(errors);

        context.Products.Add(product);
        var result = await context.SaveChangesAsync();

        return result > 0
            ? Results.Created($"/product/{product.Id}", product)
            : Results.BadRequest("There was an error registering a product");
    })
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .WithName("PostProduct")
        .WithTags("Products");

    app.MapPut("/product/{id}", [Authorize] async (Guid id, MinimalContextDb context, Product product) =>
    {
        if (!MiniValidator.TryValidate(product, out var errors))
            return Results.ValidationProblem(errors);

        var existingProduct = await context.Products.AsNoTracking<Product>()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (existingProduct is null) return Results.NotFound();

        context.Products.Update(product);
        var result = await context.SaveChangesAsync();

        return result > 0
            ? Results.NoContent()
            : Results.BadRequest("There was an error updating the product");
    })
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest)
        .WithName("PutProduct")
        .WithTags("Products");

    app.MapDelete("/product/{id}", [Authorize] async (Guid id, MinimalContextDb context) =>
    {
        var productToRemove = await context.Products.FindAsync(id);
        if (productToRemove is null) return Results.NotFound();

        context.Products.Remove(productToRemove);
        var result = await context.SaveChangesAsync();

        return result > 0
            ? Results.NoContent()
            : Results.BadRequest("There was an error removing the product");
    })
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest)
        .WithName("DeleteProduct")
        .WithTags("Products");

    app.MapPost("/register", [AllowAnonymous] async (
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IOptions<JwtAppSettings> jwtAppSettings,
        RegisterUser registerUser) =>
    {
        if (registerUser is null)
            return Results.BadRequest("User not informed");

        if (!MiniValidator.TryValidate(registerUser, out var errors))
            return Results.ValidationProblem(errors);

        var user = new IdentityUser
        {
            UserName = registerUser.UserName,
            Email = registerUser.Email
        };

        var result = await userManager.CreateAsync(user, registerUser.Password);

        if (!result.Succeeded)
            return Results.BadRequest(result.Errors);


        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtAppSettings.Value.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new JwtSecurityToken(
           signingCredentials: creds);

        return Results.Ok(new JwtSecurityTokenHandler().WriteToken(token));
    })
        .ProducesValidationProblem()
        .Produces<string>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .WithName("RegisterUser")
        .WithTags("Users");

    app.MapPost("/login", [AllowAnonymous] async (
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IOptions<JwtAppSettings> jwtAppSettings,
        LoginUser loginUser) =>
    {
        if (loginUser is null)
            return Results.BadRequest("Username or Password not informed");

        if (!MiniValidator.TryValidate(loginUser, out var errors))
            return Results.ValidationProblem(errors);

        var result = await signInManager.PasswordSignInAsync(loginUser.UserName,
            loginUser.Password, isPersistent: false, lockoutOnFailure: true);

        if (result.IsLockedOut)
            return Results.BadRequest("Blocked user");

        if (!result.Succeeded)
            return Results.BadRequest("Invalid Username or Password");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtAppSettings.Value.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new JwtSecurityToken(
           signingCredentials: creds);

        return Results.Ok(new JwtSecurityTokenHandler().WriteToken(token));
    })
        .ProducesValidationProblem()
        .Produces<string>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .WithName("LoginUser")
        .WithTags("Users");
}

#endregion