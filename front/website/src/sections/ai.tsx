import ItemList from "@/components/item-list";
import Section from "@/components/section";
import { ai } from "@/content/landing";
import styles from "./ai.module.css";

/** Artificial intelligence as a tool of the trainer, not the message: every capability says it is still to come. */
export default function Ai() {
  return (
    <Section id="ia" tone="ink" title={ai.title} lead={ai.description}>
      <div className={styles.grid}>
        <div className={styles.panel}>
          <ItemList items={ai.capabilities} onInk />
        </div>
        <p className={styles.motto}>{ai.motto}</p>
      </div>
    </Section>
  );
}
