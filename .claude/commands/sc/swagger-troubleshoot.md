# Swagger Troubleshoot

## prompt
You are an SRE and .NET WebAPI expert.  
The goal is to troubleshoot **Swagger JSON errors** (500/404/ambiguous) and provide a fix plan.  

Please do the following:

1. **Check Swagger configuration**
   - Verify `Program.cs` or `Startup.cs` has:
     - `builder.Services.AddEndpointsApiExplorer();`
     - `builder.Services.AddSwaggerGen();`
     - `app.UseSwagger();`
     - `app.UseSwaggerUI();`
   - If missing, show the correct code block.

2. **Check for SchemaId conflicts**
   - Detect if there are duplicate class names across namespaces.
   - Recommend using `c.CustomSchemaIds(t => t.FullName);`.

3. **Check Controller & Action methods**
   - Find public methods without `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]`.
   - Suggest adding correct attributes or mark them `[NonAction]`.

4. **Check package versions**
   - Confirm Swashbuckle.AspNetCore matches .NET runtime version.
   - If mismatch, recommend upgrade.

5. **Output**
   - A **step-by-step runbook**: “Symptom → Check → Fix”.
   - Example diffs for `Program.cs` and sample controller.
   - A **smoke test checklist** with `curl` commands:
     - `curl -f https://localhost:5001/swagger/v1/swagger.json`
     - Response code 200, header `application/json`, body contains `"openapi"`.

Save the output into:
- `docs/troubleshooting/swagger-json-runbook.md`
- Also summarize critical issues inline for quick reference.
