"use client";

import { type FormEvent, useReducer, useRef, useState } from "react";
import Link from "next/link";
import { track } from "@/analytics/track";
import Button from "@/components/button";
import { leadForm } from "@/content/landing";
import { fieldErrorsOf, type FieldErrors, leadFormReducer } from "@/leads/lead-form-state";
import { registerLead } from "@/leads/register-lead";
import styles from "./early-access-form.module.css";

/** The early-access form: an email and the privacy consent. Only the email is sent. */
export default function EarlyAccessForm() {
  const [state, dispatch] = useReducer(leadFormReducer, { status: "idle" });
  const [email, setEmail] = useState("");
  const [consent, setConsent] = useState(false);
  const [errors, setErrors] = useState<FieldErrors>({});
  const honeypot = useRef<HTMLInputElement>(null);
  const started = useRef(false);
  const submitting = state.status === "submitting";

  const onStart = () => {
    if (!started.current) {
      started.current = true;
      track({ name: "form_start" });
    }
  };

  const onSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (submitting) {
      return;
    }
    const found = fieldErrorsOf(email, consent);
    setErrors(found);
    if (found.email || found.consent) {
      track({ name: "form_submit", outcome: "invalid" });
      return;
    }
    // A bot filled the hidden field: it sees the same confirmation, and nothing is sent.
    if (honeypot.current?.value) {
      dispatch({ type: "sent" });
      return;
    }

    dispatch({ type: "submit" });
    const result = await registerLead(email.trim());
    if (result.ok) {
      dispatch({ type: "sent" });
      track({ name: "form_submit", outcome: "sent" });
    } else {
      dispatch({ type: "failed", reason: result.reason });
      track({ name: "form_submit", outcome: result.reason });
    }
  };

  if (state.status === "sent") {
    return (
      <div className={styles.confirmation} role="status">
        <h3>{leadForm.confirmationTitle}</h3>
        <p>{leadForm.confirmation}</p>
      </div>
    );
  }

  return (
    <form
      className={styles.form}
      noValidate
      onSubmit={onSubmit}
      aria-labelledby="formulario-titulo"
    >
      <h3 id="formulario-titulo" className={styles.title}>
        {leadForm.title}
      </h3>

      <div className={styles.field}>
        <label htmlFor="lead-email" className={styles.label}>
          {leadForm.emailLabel}
        </label>
        <input
          id="lead-email"
          className={styles.input}
          type="email"
          name="email"
          autoComplete="email"
          inputMode="email"
          placeholder={leadForm.emailPlaceholder}
          required
          value={email}
          onFocus={onStart}
          onChange={(event) => {
            setEmail(event.target.value);
            setErrors(({ consent: consentError }) => ({ consent: consentError }));
          }}
          aria-invalid={errors.email ? true : undefined}
          aria-describedby={errors.email ? "lead-email-error" : undefined}
          disabled={submitting}
        />
        {errors.email && (
          <p id="lead-email-error" className={styles.error}>
            {leadForm.errors.email}
          </p>
        )}
      </div>

      <div className={styles.honeypot} aria-hidden="true">
        <label htmlFor="lead-company">{leadForm.honeypotLabel}</label>
        <input
          id="lead-company"
          ref={honeypot}
          type="text"
          name="company"
          tabIndex={-1}
          autoComplete="off"
        />
      </div>

      <div className={styles.field}>
        <div className={styles.consent}>
          <input
            id="lead-consent"
            className={styles.checkbox}
            type="checkbox"
            name="consent"
            required
            checked={consent}
            onChange={(event) => {
              setConsent(event.target.checked);
              setErrors(({ email: emailError }) => ({ email: emailError }));
            }}
            aria-invalid={errors.consent ? true : undefined}
            aria-describedby={errors.consent ? "lead-consent-error" : undefined}
            disabled={submitting}
          />
          <label htmlFor="lead-consent">
            {leadForm.consentBefore}
            <Link href="/privacidad">{leadForm.consentLink}</Link>
            {leadForm.consentAfter}
          </label>
        </div>
        {errors.consent && (
          <p id="lead-consent-error" className={styles.error}>
            {leadForm.errors.consent}
          </p>
        )}
      </div>

      {state.status === "failed" && (
        <p className={styles.alert} role="alert">
          {leadForm.errors[state.reason]}
        </p>
      )}

      <Button
        type="submit"
        size="large"
        block
        disabled={submitting}
        aria-busy={submitting || undefined}
      >
        {submitting ? leadForm.submitting : leadForm.submit}
      </Button>
    </form>
  );
}
