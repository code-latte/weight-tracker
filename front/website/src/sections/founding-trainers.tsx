import Badge from "@/components/badge";
import ItemList from "@/components/item-list";
import { founding } from "@/content/landing";
import sectionStyles from "@/components/section.module.css";
import EarlyAccessForm from "./early-access-form";
import styles from "./founding-trainers.module.css";

/** The Founding Trainers programme and its early-access form, the page's goal. */
export default function FoundingTrainers() {
  return (
    <section
      id="acceso-anticipado"
      data-section="acceso-anticipado"
      aria-labelledby="acceso-anticipado-titulo"
      className={`${sectionStyles.section} ${sectionStyles.background}`}
    >
      <div className={sectionStyles.container}>
        <div className={styles.panel}>
          <div className={styles.copy}>
            <Badge tone="orange">{founding.badge}</Badge>
            <h2 id="acceso-anticipado-titulo" className={styles.title}>
              {founding.title}
            </h2>
            <p className={styles.description}>{founding.description}</p>
            <p className={styles.limit}>{founding.limit}</p>
            <h3 className={styles.benefitsTitle}>{founding.benefitsTitle}</h3>
            <ItemList items={founding.benefits.map((text) => ({ text, status: "available" }))} />
            <p className={styles.development}>{founding.development}</p>
          </div>
          <EarlyAccessForm />
        </div>
      </div>
    </section>
  );
}
