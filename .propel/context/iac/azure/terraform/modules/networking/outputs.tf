output "vnet_id" {
  value       = azurerm_virtual_network.main.id
  description = "ID of the Virtual Network"
}

output "vnet_name" {
  value       = azurerm_virtual_network.main.name
  description = "Name of the Virtual Network"
}

output "public_subnet_id" {
  value       = azurerm_subnet.public.id
  description = "ID of the public subnet"
}

output "app_subnet_id" {
  value       = azurerm_subnet.app.id
  description = "ID of the application subnet"
}

output "data_subnet_id" {
  value       = azurerm_subnet.data.id
  description = "ID of the data subnet"
}

output "nat_gateway_id" {
  value       = azurerm_nat_gateway.main.id
  description = "ID of the NAT Gateway"
}
