using Confidence.Iam.Types.V1;

namespace Spotify.Confidence.OpenFeature.Local.Models;
public class AccountClient
{
    public string AccountName { get; }
    public Client Client { get; }
    public ClientCredential ClientCredential { get; }

    public AccountClient(string accountName, Client client, ClientCredential clientCredential)
    {
        AccountName = accountName;
        Client = client;
        ClientCredential = clientCredential;
    }
}
