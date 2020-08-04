# Embed a bot in a web page using Web Chat

Any bot registered with the **Bot Framework** is automatically configured for the **Web Chat channel** which includes the **Web Chat control** to allow communication with the bot. As described in the [Connect a bot to Web Chat](https://docs.microsoft.com/bot-framework/channel-connect-webchat) article, a common way to embed a bot in a website is using an *iframe* HTML element. The problem with this approach is that it exposes the Web Channel secret in the client web page. This can be acceptable during development but not in a production environment.

> [!WARNING]
> In production, **it is strongly recommend that you use token retrieval instead of your secret**. See [Secrets and tokens](https://docs.microsoft.com/azure/bot-service/rest-api/bot-framework-rest-direct-line-3-0-authentication?view=azure-bot-service-4.0#secrets-and-tokens).

For more information, refer to the [Embed a bot in a web page using Web Chat](tbd) post.
