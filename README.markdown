Glimpse.RavenDb is a RavenDb profiling plugin for Glimpse.

###Install
There are two ways to hook your RavenDb DocumentStore into the plugin.

1. Explicitly via Glimpse.RavenDb.Profiler.AttachTo()
2. Web.Config via the Glimpse.RavenDb.DocumentStoreApplicationKey AppSetting.

You can also filter senstive data out of your documents by either

1. Calling Glimpse.RavenDb.Profiler.HideFields()
2. Adding a Glimpse.RavenDb.HiddenFields AppSetting.


###Example DocumentStore Creation

var store = new DocumentStore();

store.Initialize();

Application["MyDocStore"]= store;

###Explicit

Glimpse.RavenDb.Profiler.AttachTo(store);

Glimpse.RavenDb.Profiler.HideFields("PasswordHash", "PasswordSalt");

###Web.Config

`<appSettings>'

	<add key="Glimpse.RavenDb.DocumentStoreApplicationKey" value="MyDocStore" />	<!-- The key into the Application dictionary that holds your instance -->
	
	<add key="Glimpse.RavenDb.HiddenFields" value="PasswordHash,PasswordSalt" />	<!-- Comma separated -->
	
'</appSettings>`

###Learn More

RavenDb - http://www.ravendb.net

Glimpse - http://www.getglimpse.com

Chris Sainty - http://csainty.blogspot.com

