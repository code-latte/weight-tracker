import Icon from "@/components/icon";
import Section from "@/components/section";
import StatusLabel from "@/components/status-label";
import { howItWorks } from "@/content/landing";
import styles from "./how-it-works.module.css";

/** The workflow in three steps: horizontal on wide screens, stacked on small ones. */
export default function HowItWorks() {
  return (
    <Section id="como-funciona" tone="paper" title={howItWorks.title} centered>
      <ol className={styles.steps}>
        {howItWorks.steps.map((step) => (
          <li key={step.title} className={styles.step}>
            <span className={styles.number}>
              <Icon name={step.icon} />
            </span>
            <h3 className={styles.title}>
              {step.title}
              <StatusLabel status={step.status} />
            </h3>
            <p className={styles.text}>{step.text}</p>
          </li>
        ))}
      </ol>
    </Section>
  );
}
