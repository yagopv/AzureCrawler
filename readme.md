## What´s this?

If you are developing applications using new modern JavaScript frameworks like *Angular, Ember, Durandal*  ... etc. you probably already know that this type of applications are not crawlable by search engine robots without a couple of extra steps.

## SEO according to Google

If you want your JavaScript application to be crawlable, you need to implement some steps on your own. You can find information about the process on [this Google document](https://developers.google.com/webmasters/ajax-crawling/docs/getting-started?hl=iw). Take a look to it in order to understand better what is required  both client and server side.

## What is AzureCrawler about?

AzureCrawler helps with taking *HTML Snapshots* of your dynamically generated content.

This project is specific to Azure and is ready to be deployed as a Cloud Service.  *AzureCrawler is a Worker Role that uses OWIN to  Self-Host a  Web API*.  

Said that, it´s easy to bring the code to your own solution if you don´t want to use it as a separate  Cloud Service.  As well, if you are not using .NET and Azure it´s not complicated to port it to another platform like Amazon Web Services.

## How AzureCrawler works?

The self-hosted Web API contained in AzureCrawler exposes a resource with an endpoint in:

```
POST api/snapshot

```

If you make a api call there, a *PhantomJS* process will run and take care of  the *HTML Snapshot* against the provided url.

You can pass some parameters in the body of the POST call

```
string ApiId (required). The application identification
string Application (required). The application name
string Url (required). The url to crawl
bool Store (optional). If you want to store the snapshot for future calls
DateTime ExpirationDate (optional). The expiration of  the stored snapshot
string UserAgent (optional). The user agent of the bot crawling your application

```

*ApiId* and *Application* fields are required and will be validated together. 

There isn´t any special mechanism for doing this validation more than the following private method:

```
/// <summary>
/// Validate ApiKey. In the real world you should this against a custom store
/// </summary>
/// <param name="apiKey">The api key</param>
/// <param name="apiKey">The application</param>
/// <returns>bool</returns>
private bool ValidateCredentials(string apiKey, string application)
{
    if (apiKey == "Any ApiId" && application == "Any Application name")
    {
        return true;
    }
    return false;
}
```

So you can supply a new mechanism, use your own keys or use a database to store application credentials.

The *Url* is the resource you want to crawl. The PhantomJS process will take care of  the snapshot  and will wait until all the dynamically generated content will be loaded.

The latest fields are about providing information for storing the HTML  Snapshot in the store you prefer to.  

By default, AzureCrawler will store the snapshots in Azure Storage within a blob container with the name of the *Application* field. 

If you do this, next time a bot requests the same *Url*,  the snapshot will be provided from the storage.  

When the snapshot stored expires, a new crawl will be done and a new snapshot will be stored.

## Know issues

There is a [incompatibility](http://stackoverflow.com/questions/20258352/net-azure-sdk-blob-request-returns-400-badrequest) between the Azure Compute Emulator included in the SDK 2.2. and the latest 3.x Storage assemblies so you should test with live containers until next Azure toolkit will be released.
