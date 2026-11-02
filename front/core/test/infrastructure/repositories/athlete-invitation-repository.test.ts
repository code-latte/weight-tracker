import { describe, expect, it } from "vitest";
import type { HttpClientInterface, HttpRequest } from "../../../src/application";
import { type AthleteInvitation, type CreatedAthleteInvitation, ok } from "../../../src/domain";
import { AthleteInvitationRepository, HttpError } from "../../../src/infrastructure";

const invitationId = "5e6f7a8b-9c0d-4e1f-8a2b-3c4d5e6f7a8b";

const invitation: AthleteInvitation = {
  id: invitationId,
  trainerId: "3c4d5e6f-7a8b-4c9d-8e0f-1a2b3c4d5e6f",
  trainerName: "Ana",
  trainerSurname: "García",
  inviteEmail: "luis@example.com",
  userId: null,
  userName: null,
  userSurname: null,
  status: "PENDING",
  createdAtUtc: "2026-10-28T10:00:00Z",
  expiresAtUtc: "2026-11-04T10:00:00Z",
  acceptedAtUtc: null,
};

const created: CreatedAthleteInvitation = {
  id: invitation.id,
  trainerId: invitation.trainerId,
  trainerName: invitation.trainerName,
  trainerSurname: invitation.trainerSurname,
  inviteEmail: invitation.inviteEmail,
  userId: null,
  userName: null,
  userSurname: null,
  status: "PENDING",
  createdAtUtc: invitation.createdAtUtc,
  expiresAtUtc: invitation.expiresAtUtc,
  token: "raw-token",
};

class FakeHttpClient implements HttpClientInterface {
  readonly requests: HttpRequest[] = [];

  constructor(private readonly respond: () => Promise<unknown>) {}

  async request<T>(httpRequest: HttpRequest): Promise<T> {
    this.requests.push(httpRequest);
    return (await this.respond()) as T;
  }
}

describe("AthleteInvitationRepository", () => {
  describe("getAll", () => {
    it("sends GET /athlete-invitations without a body", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => [invitation]);
      const repository = new AthleteInvitationRepository(httpClient);

      // Act
      await repository.getAll();

      // Assert
      expect(httpClient.requests).toEqual([{ method: "GET", path: "/athlete-invitations" }]);
    });

    it("returns what the API answers on success", async () => {
      // Arrange
      const repository = new AthleteInvitationRepository(
        new FakeHttpClient(async () => [invitation]),
      );

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual(ok([invitation]));
    });

    it.each([
      [401, "UNAUTHORIZED"],
      [403, "FORBIDDEN"],
      [500, "UNEXPECTED"],
    ])("maps status %i to %s", async (status, code) => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new HttpError(status, { status, detail: "Something failed" });
      });
      const repository = new AthleteInvitationRepository(httpClient);

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual({
        ok: false,
        error: expect.objectContaining({ code, message: "Something failed" }),
      });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new TypeError("Failed to fetch");
      });
      const repository = new AthleteInvitationRepository(httpClient);

      // Act
      const result = await repository.getAll();

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("invite", () => {
    it("sends POST /athlete-invitations with the email unchanged", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => created);
      const repository = new AthleteInvitationRepository(httpClient);

      // Act
      await repository.invite({ email: "luis@example.com" });

      // Assert
      expect(httpClient.requests).toEqual([
        { method: "POST", path: "/athlete-invitations", body: { email: "luis@example.com" } },
      ]);
    });

    it("returns what the API answers on success", async () => {
      // Arrange
      const repository = new AthleteInvitationRepository(new FakeHttpClient(async () => created));

      // Act
      const result = await repository.invite({ email: "luis@example.com" });

      // Assert
      expect(result).toEqual(ok(created));
    });

    it.each([
      [400, "VALIDATION"],
      [401, "UNAUTHORIZED"],
      [403, "FORBIDDEN"],
      [409, "CONFLICT"],
      [500, "UNEXPECTED"],
    ])("maps status %i to %s", async (status, code) => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new HttpError(status, { status, detail: "Something failed" });
      });
      const repository = new AthleteInvitationRepository(httpClient);

      // Act
      const result = await repository.invite({ email: "luis@example.com" });

      // Assert
      expect(result).toEqual({
        ok: false,
        error: expect.objectContaining({ code, message: "Something failed" }),
      });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new TypeError("Failed to fetch");
      });
      const repository = new AthleteInvitationRepository(httpClient);

      // Act
      const result = await repository.invite({ email: "luis@example.com" });

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("revoke", () => {
    it("sends PUT /athlete-invitations/{id}/revoke without a body", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => invitation);
      const repository = new AthleteInvitationRepository(httpClient);

      // Act
      await repository.revoke(invitationId);

      // Assert
      expect(httpClient.requests).toEqual([
        { method: "PUT", path: `/athlete-invitations/${invitationId}/revoke` },
      ]);
    });

    it("returns what the API answers on success", async () => {
      // Arrange
      const repository = new AthleteInvitationRepository(
        new FakeHttpClient(async () => invitation),
      );

      // Act
      const result = await repository.revoke(invitationId);

      // Assert
      expect(result).toEqual(ok(invitation));
    });

    it.each([
      [401, "UNAUTHORIZED"],
      [403, "FORBIDDEN"],
      [404, "NOT_FOUND"],
      [409, "CONFLICT"],
      [500, "UNEXPECTED"],
    ])("maps status %i to %s", async (status, code) => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new HttpError(status, { status, detail: "Something failed" });
      });
      const repository = new AthleteInvitationRepository(httpClient);

      // Act
      const result = await repository.revoke(invitationId);

      // Assert
      expect(result).toEqual({
        ok: false,
        error: expect.objectContaining({ code, message: "Something failed" }),
      });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new TypeError("Failed to fetch");
      });
      const repository = new AthleteInvitationRepository(httpClient);

      // Act
      const result = await repository.revoke(invitationId);

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("accept", () => {
    it("sends PUT /athlete-invitations/{id}/accept with the token", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => invitation);
      const repository = new AthleteInvitationRepository(httpClient);

      // Act
      await repository.accept(invitationId, { token: "raw-token" });

      // Assert
      expect(httpClient.requests).toEqual([
        {
          method: "PUT",
          path: `/athlete-invitations/${invitationId}/accept`,
          body: { token: "raw-token" },
        },
      ]);
    });

    it("returns what the API answers on success", async () => {
      // Arrange
      const repository = new AthleteInvitationRepository(
        new FakeHttpClient(async () => invitation),
      );

      // Act
      const result = await repository.accept(invitationId, { token: "raw-token" });

      // Assert
      expect(result).toEqual(ok(invitation));
    });

    it.each([
      [400, "VALIDATION"],
      [401, "UNAUTHORIZED"],
      [403, "FORBIDDEN"],
      [404, "NOT_FOUND"],
      [409, "CONFLICT"],
      [500, "UNEXPECTED"],
    ])("maps status %i to %s", async (status, code) => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new HttpError(status, { status, detail: "Something failed" });
      });
      const repository = new AthleteInvitationRepository(httpClient);

      // Act
      const result = await repository.accept(invitationId, { token: "raw-token" });

      // Assert
      expect(result).toEqual({
        ok: false,
        error: expect.objectContaining({ code, message: "Something failed" }),
      });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new TypeError("Failed to fetch");
      });
      const repository = new AthleteInvitationRepository(httpClient);

      // Act
      const result = await repository.accept(invitationId, { token: "raw-token" });

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });

  describe("reject", () => {
    it("sends PUT /athlete-invitations/{id}/reject with the token", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => invitation);
      const repository = new AthleteInvitationRepository(httpClient);

      // Act
      await repository.reject(invitationId, { token: "raw-token" });

      // Assert
      expect(httpClient.requests).toEqual([
        {
          method: "PUT",
          path: `/athlete-invitations/${invitationId}/reject`,
          body: { token: "raw-token" },
        },
      ]);
    });

    it("returns what the API answers on success", async () => {
      // Arrange
      const repository = new AthleteInvitationRepository(
        new FakeHttpClient(async () => invitation),
      );

      // Act
      const result = await repository.reject(invitationId, { token: "raw-token" });

      // Assert
      expect(result).toEqual(ok(invitation));
    });

    it.each([
      [400, "VALIDATION"],
      [401, "UNAUTHORIZED"],
      [403, "FORBIDDEN"],
      [404, "NOT_FOUND"],
      [409, "CONFLICT"],
      [500, "UNEXPECTED"],
    ])("maps status %i to %s", async (status, code) => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new HttpError(status, { status, detail: "Something failed" });
      });
      const repository = new AthleteInvitationRepository(httpClient);

      // Act
      const result = await repository.reject(invitationId, { token: "raw-token" });

      // Assert
      expect(result).toEqual({
        ok: false,
        error: expect.objectContaining({ code, message: "Something failed" }),
      });
    });

    it("returns the network code when there is no response", async () => {
      // Arrange
      const httpClient = new FakeHttpClient(async () => {
        throw new TypeError("Failed to fetch");
      });
      const repository = new AthleteInvitationRepository(httpClient);

      // Act
      const result = await repository.reject(invitationId, { token: "raw-token" });

      // Assert
      expect(result).toEqual({ ok: false, error: expect.objectContaining({ code: "NETWORK" }) });
    });
  });
});
