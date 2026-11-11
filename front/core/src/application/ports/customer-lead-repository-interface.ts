import type { CustomerLead, DomainError, Result } from "../../domain";

export default interface CustomerLeadRepositoryInterface {
  getAll(): Promise<Result<CustomerLead[], DomainError>>;
}
