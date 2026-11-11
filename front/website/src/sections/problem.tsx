import Card from "@/components/card";
import Icon from "@/components/icon";
import Section from "@/components/section";
import { problem } from "@/content/landing";
import styles from "./problem.module.css";

/** The trainer's problems today, before any feature: "esto me pasa constantemente". */
export default function Problem() {
  return (
    <Section id="problema" tone="fog" title={problem.title} lead={problem.description}>
      <ul className={styles.grid}>
        {problem.cards.map((card) => (
          <li key={card.title}>
            <Card>
              <span className={styles.icon}>
                <Icon name={card.icon} />
              </span>
              <h3 className={styles.cardTitle}>{card.title}</h3>
              <p className={styles.cardText}>{card.text}</p>
            </Card>
          </li>
        ))}
      </ul>
      <p className={styles.closing}>{problem.closing}</p>
    </Section>
  );
}
