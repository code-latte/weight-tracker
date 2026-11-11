import type { CustomerLeadRepositoryInterface, HttpClientInterface } from "../../application";
import { type CustomerLead, type DomainError, fail, ok, type Result } from "../../domain";
import { mapHttpError } from "../http";

export default class CustomerLeadRepository implements CustomerLeadRepositoryInterface {
  constructor(private readonly httpClient: HttpClientInterface) {}

  async getAll(): Promise<Result<CustomerLead[], DomainError>> {
    try {
      const response = await this.httpClient.request<CustomerLead[]>({
        method: "GET",
        path: "/customer-leads",
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }
}
