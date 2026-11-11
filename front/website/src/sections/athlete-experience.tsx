import Card from "@/components/card";
import ItemList from "@/components/item-list";
import ProductImage from "@/components/product-image";
import Section from "@/components/section";
import { experience } from "@/content/landing";
import styles from "./athlete-experience.module.css";

/** "Experiencia del entrenador y del deportista": the trainer's benefits beside the athlete's app. */
export default function AthleteExperience() {
  return (
    <Section id="para-entrenadores" tone="background" title={experience.title}>
      <div className={styles.grid}>
        <div>
          <h3 className={styles.columnTitle}>{experience.trainer.title}</h3>
          <ul className={styles.benefits}>
            {experience.trainer.benefits.map((benefit) => (
              <li key={benefit.title} className={styles.benefit}>
                <strong>{benefit.title}</strong>
                <span>{benefit.text}</span>
              </li>
            ))}
          </ul>
        </div>
        <Card flat>
          <div className={styles.athlete}>
            <div className={styles.athleteCopy}>
              <h3 className={styles.columnTitle}>{experience.athlete.title}</h3>
              <p>{experience.athlete.description}</p>
              <ItemList items={experience.athlete.items} />
            </div>
            <div className={styles.phone}>
              <ProductImage image={experience.athlete.image} device="phone" />
            </div>
          </div>
        </Card>
      </div>
    </Section>
  );
}
