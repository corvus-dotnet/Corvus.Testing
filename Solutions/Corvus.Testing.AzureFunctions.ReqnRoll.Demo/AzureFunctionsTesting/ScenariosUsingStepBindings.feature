Feature: Using step bindings
	In order to test my Azure functions
	As a developer
	I want to be able to start an Azure function from a step in my Scenario

Scenario Outline: A Get request including a name in the querystring is successful
	Given I start a functions instance for the local project '<function>' on port 7075 with runtime '<runtime>'
	When I send a GET request to 'http://localhost:7075/?name=Jon'
	Then I receive a 200 response code
	And the response body contains the text 'Hello, Jon'

	Examples: 
	| function                                             | runtime |
	| Corvus.Testing.AzureFunctions.DemoFunction.InProcess | net6.0  |
	| Corvus.Testing.AzureFunctions.DemoFunctions.Isolated | net8.0  |

Scenario Outline: A Get request without providing a name in the querystring fails.
	Given I start a functions instance for the local project '<function>' on port 7075 with runtime '<runtime>'
	When I send a GET request to 'http://localhost:7075/'
	Then I receive a 400 response code

	Examples: 
	| function | runtime |
	| Corvus.Testing.AzureFunctions.DemoFunction.InProcess | net6.0 |
    | Corvus.Testing.AzureFunctions.DemoFunctions.Isolated | net8.0 |

Scenario Outline: A Post request including a name in the querystring is successful
	Given I start a functions instance for the local project '<function>' on port 7075 with runtime '<runtime>'
	When I send a POST request to 'http://localhost:7075/?name=Jon'
	Then I receive a 200 response code
	And the response body contains the text 'Hello, Jon'

	Examples: 
	| function | runtime |
	| Corvus.Testing.AzureFunctions.DemoFunction.InProcess | net6.0 |
    | Corvus.Testing.AzureFunctions.DemoFunctions.Isolated | net8.0 |

Scenario Outline: A Post request including a name in the request body is successful
	Given I start a functions instance for the local project '<function>' on port 7075 with runtime '<runtime>'
	When I send a POST request to 'http://localhost:7075/' with data in the request body
	| PropertyName | Value |
	| name         | Jon   |
	Then I receive a 200 response code
	And the response body contains the text 'Hello, Jon'

	Examples: 
	| function | runtime |
	| Corvus.Testing.AzureFunctions.DemoFunction.InProcess | net6.0 |
    | Corvus.Testing.AzureFunctions.DemoFunctions.Isolated | net8.0 |

Scenario Outline: A Post request including names in the querystring and request body uses the name in the querystring
	Given I start a functions instance for the local project '<function>' on port 7075 with runtime '<runtime>'
	When I send a POST request to 'http://localhost:7075/?name=Jon' with data in the request body
	| PropertyName | Value    |
	| name         | Jonathan |
	Then I receive a 200 response code
	And the response body contains the text 'Hello, Jon'

	Examples: 
	| function | runtime |
	| Corvus.Testing.AzureFunctions.DemoFunction.InProcess | net6.0 |
    | Corvus.Testing.AzureFunctions.DemoFunctions.Isolated | net8.0 |

Scenario Outline: A Post request without a querystring or request body fails
	Given I start a functions instance for the local project '<function>' on port 7075 with runtime '<runtime>'
	When I send a POST request to 'http://localhost:7075/'
	Then I receive a 400 response code

	Examples: 
	| function | runtime |
	| Corvus.Testing.AzureFunctions.DemoFunction.InProcess | net6.0 |
    | Corvus.Testing.AzureFunctions.DemoFunctions.Isolated | net8.0 |

Scenario Outline: Supplying an alternative greeting via configuration
	Given I have set additional configuration for functions instances
	| Key             | Value           |
	| ResponseMessage | Welcome, {name} |
	Given I start a functions instance for the local project '<function>' on port 7075 with runtime '<runtime>'
	When I send a GET request to 'http://localhost:7075/?name=Jon'
	Then I receive a 200 response code
	And the response body contains the text 'Welcome, Jon'

	Examples: 
	| function | runtime |
	| Corvus.Testing.AzureFunctions.DemoFunction.InProcess | net6.0 |
    | Corvus.Testing.AzureFunctions.DemoFunctions.Isolated | net8.0 |
