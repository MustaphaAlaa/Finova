if [ ! -d "Wallet" ]; then
	mkdir Wallet
fi
cd ./Wallet

dotnet tool install --global dotnet-ef

if [ ! -f "Finova.sln" ]; then
	dotnet new sln -n Finova
fi

if [ ! -d "./Host/Finova.API" ]; then
	mkdir ./Host ./Modules
	dotnet new webapi -n Finova.API -o ./Host/Finova.API
	dotnet sln add ./Host/Finova.API
fi


add_efcore_packages(){
        local projName=$1
    	local module=$2 
		dotnet add "$module" package Npgsql.EntityFrameworkCore.PostgreSQL
		dotnet add "$module"  package Microsoft.EntityFrameworkCore
		dotnet add "$module"  package Microsoft.EntityFrameworkCore.Design
		touch "$module/$projName".DbContext.cs
}

add_projects_references(){
	    local projName=$1
    	local module=$2

		#Web API -> Application
		dotnet add  "./Host/Finova.API/Finova.API.csproj" reference "$module/Application/$projName.Application.csproj"
		
		# Application -> Domain
		dotnet add "$module/Application/$projName.Application.csproj" reference "$module/Domain/$projName.Domain.csproj"
		
		# Application -> Contracts
		dotnet add "$module/Application/$projName.Application.csproj" reference "$module/Contracts/$projName.Contracts.csproj"

		# Infrastructure -> Application
		dotnet add "$module/Infrastructure/$projName.Infrastructure.csproj" reference "$module/Application/$projName.Application.csproj"

		# Presentation -> Application
		dotnet add "$module/Presentation/$projName.Presentation.csproj" reference "$module/Application/$projName.Application.csproj"

		# Presentation -> Infrastructure
		dotnet add "$module/Presentation/$projName.Presentation.csproj" reference "$module/Infrastructure/$projName.Infrastructure.csproj"
}

create_module() {
    local projName=$1
    local module=$2

		for layer in Domain Application Infrastructure Presentation Contracts
		do
			if [ ! -d "$module/$layer" ]; then
				
				dotnet new classlib -n "$projName.$layer" -o "$module/$layer"
				
				if [[ "$projName" == "Identity.Module" && "$layer" == "Domain" ]]; then 
					dotnet add "$module/$layer"  package Microsoft.AspNetCore.Identity.EntityFrameworkCore 
				fi

				if [[ "$layer" == "Infrastructure" ]]; then 
					add_efcore_packages $projName "$module/$layer" 
				fi
				
				dotnet sln add "$module/$layer"
			fi
    		done

		add_projects_references $projName $module
}


create_module SendMoney 		./Modules/SendMoney.Module/   		
create_module Banks    			./Modules/Banks.Module/    
create_module ReceiveMoney      ./Modules/ReceiveMoney.Module/
create_module Identity   	    ./Modules/Identity.Module/
