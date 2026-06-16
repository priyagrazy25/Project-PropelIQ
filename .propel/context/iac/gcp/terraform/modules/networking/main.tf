locals {
  name_prefix = "${var.project_name}-${var.environment}"
}

# INFRA-010: VPC Network
resource "google_compute_network" "main" {
  name                    = "${local.name_prefix}-vpc"
  project                 = var.project_id
  auto_create_subnetworks = false
}

# INFRA-011: Application subnet (private)
resource "google_compute_subnetwork" "app" {
  name                     = "${local.name_prefix}-subnet-app"
  project                  = var.project_id
  ip_cidr_range            = "10.0.2.0/24"
  region                   = var.region
  network                  = google_compute_network.main.id
  private_ip_google_access = true # Required for Cloud Run VPC connector
}

# INFRA-011: Data subnet (private)
resource "google_compute_subnetwork" "data" {
  name                     = "${local.name_prefix}-subnet-data"
  project                  = var.project_id
  ip_cidr_range            = "10.0.3.0/24"
  region                   = var.region
  network                  = google_compute_network.main.id
  private_ip_google_access = true
}

# INFRA-013: Firewall — deny all ingress by default
resource "google_compute_firewall" "deny_all_ingress" {
  name    = "${local.name_prefix}-fw-deny-all"
  project = var.project_id
  network = google_compute_network.main.name

  deny {
    protocol = "all"
  }

  direction     = "INGRESS"
  source_ranges = ["0.0.0.0/0"]
  priority      = 65534
}

# INFRA-013: Allow HTTPS from load balancer health checks
resource "google_compute_firewall" "allow_health_checks" {
  name    = "${local.name_prefix}-fw-allow-hc"
  project = var.project_id
  network = google_compute_network.main.name

  allow {
    protocol = "tcp"
    ports    = ["443", "8080"]
  }

  direction     = "INGRESS"
  source_ranges = ["130.211.0.0/22", "35.191.0.0/16"] # GCP health check ranges
  priority      = 100
}

# INFRA-013: Allow app-to-data internal traffic
resource "google_compute_firewall" "allow_app_to_data" {
  name    = "${local.name_prefix}-fw-app-data"
  project = var.project_id
  network = google_compute_network.main.name

  allow {
    protocol = "tcp"
    ports    = ["1433", "6379"]
  }

  direction     = "INGRESS"
  source_ranges = ["10.0.2.0/24"]
  target_tags   = ["data-tier"]
  priority      = 200
}

# INFRA-015: Cloud NAT for outbound internet from private subnets
resource "google_compute_router" "main" {
  name    = "${local.name_prefix}-router"
  project = var.project_id
  region  = var.region
  network = google_compute_network.main.id
}

resource "google_compute_router_nat" "main" {
  name                               = "${local.name_prefix}-nat"
  project                            = var.project_id
  router                             = google_compute_router.main.name
  region                             = var.region
  nat_ip_allocate_option             = "AUTO_ONLY"
  source_subnetwork_ip_ranges_to_nat = "ALL_SUBNETWORKS_ALL_IP_RANGES"
}
