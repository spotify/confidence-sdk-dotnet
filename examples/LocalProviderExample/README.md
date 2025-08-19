# Local Provider Example

This example demonstrates how to use the Confidence Local Provider with environment variable configuration.

## Setup

1. **Configure Environment Variables**:
   ```bash
   # Copy the example file
   cp .env.example .env
   
   # Edit .env with your actual credentials
   CONFIDENCE_CLIENT_ID=your-actual-client-id
   CONFIDENCE_CLIENT_SECRET=your-actual-client-secret
   CONFIDENCE_RESOLVER_CLIENT_SECRET=your-resolver-client-secret 
   ```

2. **Run the Example**:
   ```bash
   dotnet run
   ```

## Environment Variables

| Variable | Required | Description |
|----------|----------|-------------|
| `CONFIDENCE_CLIENT_ID` | ✅ | Your Confidence API client ID |
| `CONFIDENCE_CLIENT_SECRET` | ✅ | Your Confidence API client secret for state fetching |
| `CONFIDENCE_RESOLVER_CLIENT_SECRET` | ❌ | Optional separate client secret for resolve operations. If not provided, uses the main client secret |

## Security Notes

- The `.env` file is automatically ignored by Git to prevent accidental credential commits
- Never commit actual credentials to version control
- Use `.env.example` as a template for required environment variables

## Example Output

When properly configured, you should see:

```
info: Program[0]
      Starting Local Provider Example
info: Program[0]
      Loaded credentials from environment variables
info: Program[0]
      Using separate client secret for resolver operations
info: Spotify.Confidence.OpenFeature.Local.ConfidenceLocalProvider[3000]
      ConfidenceLocalProvider initialized
...
```

If environment variables are missing, you'll see clear error messages:

```
CONFIDENCE_CLIENT_ID environment variable is required. Please set it in your .env file.
```
