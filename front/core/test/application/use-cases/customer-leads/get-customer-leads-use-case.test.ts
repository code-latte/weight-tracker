import { describe, expect, it } from "vitest";
import type { CustomerLeadRepositoryInterface } from "../../../../src/application";
import { GetCustomerLeadsUseCase } from "../../../../src/application";
import { type CustomerLead, DomainError, fail, ok, type Result } from "../../../../src/domain";

const lead: CustomerLead = { email: "ana@example.com", registeredAtUtc: "2026-11-10T08:15:00Z" };

class FakeCustomerLeadRepository implements CustomerLeadRepositoryInterface {
  listCalls = 0;

  constructor(private readonly result: Result<CustomerLead[], DomainError>) {}

  async getAll(): Promise<Result<CustomerLead[], DomainError>> {
    this.listCalls += 1;
    return this.result;
  }
}

describe("GetCustomerLeadsUseCase", () => {
  it("returns the leads the repository answers with", async () => {
    // Arrange
    const useCase = new GetCustomerLeadsUseCase(new FakeCustomerLeadRepository(ok([lead])));

    // Act
    const result = await useCase.execute();

    // Assert
    expect(result).toEqual(ok([lead]));
  });

  it("lists the leads once", async () => {
    // Arrange
    const repository = new FakeCustomerLeadRepository(ok([]));
    const useCase = new GetCustomerLeadsUseCase(repository);

    // Act
    await useCase.execute();

    // Assert
    expect(repository.listCalls).toBe(1);
  });

  it.each(["UNAUTHORIZED", "FORBIDDEN", "UNEXPECTED"] as const)(
    "returns the repository failure %s unchanged",
    async (code) => {
      // Arrange
      const failure = fail(new DomainError(code, "Rejected"));
      const useCase = new GetCustomerLeadsUseCase(new FakeCustomerLeadRepository(failure));

      // Act
      const result = await useCase.execute();

      // Assert
      expect(result).toBe(failure);
    },
  );
});
