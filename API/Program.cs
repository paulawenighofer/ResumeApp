using API.Services;
using Shared.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<InMemoryResumeStore>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.MapGet("/api/", () => "Hello World");

var educations = app.MapGroup("/api/educations");
educations.MapGet("/", (InMemoryResumeStore store) => Results.Ok(store.Educations));
educations.MapGet("/{id:int}", (int id, InMemoryResumeStore store) =>
{
    var education = store.Educations.FirstOrDefault(x => x.Id == id);
    return education is null ? Results.NotFound() : Results.Ok(education);
});
educations.MapPost("/", (Education education, InMemoryResumeStore store) =>
{
    education.Id = store.NextEducationId();
    store.Educations.Add(education);
    return Results.Created($"/api/educations/{education.Id}", education);
});
educations.MapPut("/{id:int}", (int id, Education education, InMemoryResumeStore store) =>
{
    var index = store.Educations.FindIndex(x => x.Id == id);
    if (index < 0)
    {
        return Results.NotFound();
    }

    education.Id = id;
    store.Educations[index] = education;
    return Results.Ok(education);
});
educations.MapDelete("/{id:int}", (int id, InMemoryResumeStore store) =>
{
    var education = store.Educations.FirstOrDefault(x => x.Id == id);
    if (education is null)
    {
        return Results.NotFound();
    }

    store.Educations.Remove(education);
    return Results.NoContent();
});

var experiences = app.MapGroup("/api/experiences");
experiences.MapGet("/", (InMemoryResumeStore store) => Results.Ok(store.Experiences));
experiences.MapGet("/{id:int}", (int id, InMemoryResumeStore store) =>
{
    var experience = store.Experiences.FirstOrDefault(x => x.Id == id);
    return experience is null ? Results.NotFound() : Results.Ok(experience);
});
experiences.MapPost("/", (Experience experience, InMemoryResumeStore store) =>
{
    experience.Id = store.NextExperienceId();
    store.Experiences.Add(experience);
    return Results.Created($"/api/experiences/{experience.Id}", experience);
});
experiences.MapPut("/{id:int}", (int id, Experience experience, InMemoryResumeStore store) =>
{
    var index = store.Experiences.FindIndex(x => x.Id == id);
    if (index < 0)
    {
        return Results.NotFound();
    }

    experience.Id = id;
    store.Experiences[index] = experience;
    return Results.Ok(experience);
});
experiences.MapDelete("/{id:int}", (int id, InMemoryResumeStore store) =>
{
    var experience = store.Experiences.FirstOrDefault(x => x.Id == id);
    if (experience is null)
    {
        return Results.NotFound();
    }

    store.Experiences.Remove(experience);
    return Results.NoContent();
});

var skills = app.MapGroup("/api/skills");
skills.MapGet("/", (InMemoryResumeStore store) => Results.Ok(store.Skills));
skills.MapGet("/{id:int}", (int id, InMemoryResumeStore store) =>
{
    var skill = store.Skills.FirstOrDefault(x => x.Id == id);
    return skill is null ? Results.NotFound() : Results.Ok(skill);
});
skills.MapPost("/", (Skill skill, InMemoryResumeStore store) =>
{
    skill.Id = store.NextSkillId();
    store.Skills.Add(skill);
    return Results.Created($"/api/skills/{skill.Id}", skill);
});
skills.MapPut("/{id:int}", (int id, Skill skill, InMemoryResumeStore store) =>
{
    var index = store.Skills.FindIndex(x => x.Id == id);
    if (index < 0)
    {
        return Results.NotFound();
    }

    skill.Id = id;
    store.Skills[index] = skill;
    return Results.Ok(skill);
});
skills.MapDelete("/{id:int}", (int id, InMemoryResumeStore store) =>
{
    var skill = store.Skills.FirstOrDefault(x => x.Id == id);
    if (skill is null)
    {
        return Results.NotFound();
    }

    store.Skills.Remove(skill);
    return Results.NoContent();
});

var projects = app.MapGroup("/api/projects");
projects.MapGet("/", (InMemoryResumeStore store) => Results.Ok(store.Projects));
projects.MapGet("/{id:int}", (int id, InMemoryResumeStore store) =>
{
    var project = store.Projects.FirstOrDefault(x => x.Id == id);
    return project is null ? Results.NotFound() : Results.Ok(project);
});
projects.MapPost("/", (ResumeProject project, InMemoryResumeStore store) =>
{
    project.Id = store.NextProjectId();
    store.Projects.Add(project);
    return Results.Created($"/api/projects/{project.Id}", project);
});
projects.MapPut("/{id:int}", (int id, ResumeProject project, InMemoryResumeStore store) =>
{
    var index = store.Projects.FindIndex(x => x.Id == id);
    if (index < 0)
    {
        return Results.NotFound();
    }

    project.Id = id;
    store.Projects[index] = project;
    return Results.Ok(project);
});
projects.MapDelete("/{id:int}", (int id, InMemoryResumeStore store) =>
{
    var project = store.Projects.FirstOrDefault(x => x.Id == id);
    if (project is null)
    {
        return Results.NotFound();
    }

    store.Projects.Remove(project);
    return Results.NoContent();
});

app.Run();
