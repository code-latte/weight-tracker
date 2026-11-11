import type { CustomerLead, DomainError, Result } from "../../../domain";
import type { CustomerLeadRepositoryInterface, UseCaseInterface } from "../../ports";

/** Lists every customer lead. Only an admin may read them, which the API decides. */
export default class GetCustomerLeadsUseCase implements UseCaseInterface<void, CustomerLead[]> {
  constructor(private readonly customerLeadRepository: CustomerLeadRepositoryInterface) {}

  execute(): Promise<Result<CustomerLead[], DomainError>> {
    return this.customerLeadRepository.getAll();
  }
}
