# eTokenTool
A helper tool to handle eTokens for unattended server side code signing

## Command-line

General command-line:
`eTokenTool <command> [-config <configname>] <command switches>`

The switch `-config <configname>` is supported by all commands and overrides the location to the configuration file. If the switch is present the `<configname>` will be the used config file (should be and absolute path)
Otherwise the config file `%appdata%\eTokenTool\eTokenTool.cfg`

### Add command
  `eTokenTool add -token <container-id> -password <password> [-alias <alias-name>] [-machine]`

Adds a new token to the config. 
The parameter `-token` expects the value named 'Container Name' in the SafeNet UI when you click on a specific certificate.
The parameter `-password` is the password required to use that certificate on the token.
The optional parameter `-alias` allows you to give the certificate a better to remember name than the 'Container Name'
If the optional parameter `-machine` is present the password is encrypted with the windows machine key, otherwise the user key of the currently logged in user is used.

### Remove command
  `eTokenTool remove -id <container-id | alias-name>`

Removes the given token from the configuration.
The `-id` parameter is either the container-id or a given alias.

### List command
  `eTokenTool list`
  
lists all tokens stored in the configuration.

### Test command
  `eTokenTool test [-id <container-id | alias-name>]`
  
Tries to access the given entry or all entries.
The `-id` parameter is either the container-id or a given alias. If not given all entries in the configuration are tested

### Login command
  `eTokenTool login  [-id <container-id | alias-name>]`
Sets the password for the given entry or all entries.
The `-id` parameter is either the container-id or a given alias. If not given the login id done for all entries in the configuration.




