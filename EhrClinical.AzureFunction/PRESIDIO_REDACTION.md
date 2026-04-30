# Local Presidio redaction

The Azure Function can redact PHI with Microsoft Presidio before sending clinical text to Azure OpenAI. The .NET worker starts `Scripts/presidio_redact.py` as a local Python process and passes text through stdin/stdout, so the redaction stays on the Azure Function VM.

## Local setup

From `EhrClinical.AzureFunction`:

```powershell
python -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r Scripts\requirements-presidio.txt
.\.venv\Scripts\python.exe -m spacy download en_core_web_sm
```

Add these values to `local.settings.json` under `Values`:

```json
"PhiRedaction:Provider": "Presidio",
"PhiRedaction:PresidioPythonExecutable": ".venv\\Scripts\\python.exe",
"PhiRedaction:PresidioTimeoutSeconds": "30",
"PhiRedaction:FailOnPresidioError": "true"
```

Use `PhiRedaction:FailOnPresidioError=true` when testing the Presidio path. That prevents accidental fallback to regex if the Python environment is missing.

## Test endpoints

Run the function locally, then call:

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:7071/api/test/pii-redaction?code=<function-key>" `
  -ContentType "application/json" `
  -Body '{ "text": "Patient John Smith, DOB 01/02/1980, phone 212-555-0199, email john@example.com." }'
```

The response includes `redactedText`, `provider`, and `usedFallback`. This endpoint does not call Azure OpenAI.

To force a specific local redaction provider for the same input:

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:7071/api/test/pii-redaction/presidio?code=<function-key>" `
  -ContentType "application/json" `
  -Body '{ "text": "Patient John Smith, DOB 01/02/1980, phone 212-555-0199, email john@example.com." }'
```

Use `regex` instead of `presidio` to compare the local regex scrubber:

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:7071/api/test/pii-redaction/regex?code=<function-key>" `
  -ContentType "application/json" `
  -Body '{ "text": "Patient John Smith, DOB 01/02/1980, phone 212-555-0199, email john@example.com." }'
```

To retrieve the unsanitized prompt bundle for a client without calling Azure OpenAI:

```powershell
Invoke-RestMethod `
  -Method Get `
  -Uri "http://localhost:7071/api/test/clients/359402/unsanitized-prompt?code=<function-key>"
```

That response intentionally contains raw clinical text. Keep it local and do not paste it into external tools.
