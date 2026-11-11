import Icon from "@/components/icon";
import ItemList from "@/components/item-list";
import ProductImage from "@/components/product-image";
import Section from "@/components/section";
import StatusLabel from "@/components/status-label";
import { features } from "@/content/landing";
import styles from "./features.module.css";

/** The main capabilities, each with its product image and what it includes, available or coming soon. */
export default function Features() {
  return (
    <Section id="funcionalidades" tone="paper" title={features.title} centered>
      <div className={styles.blocks}>
        {features.blocks.map((block) => {
          // A block whose every item is still to come is itself still to come.
          const blockStatus = block.items.every((item) => item.status === "soon")
            ? "soon"
            : "available";
          const phone = block.image.width < block.image.height;

          return (
            <article key={block.id} className={styles.block} aria-labelledby={`${block.id}-titulo`}>
              <div className={styles.copy}>
                <span className={styles.icon}>
                  <Icon name={block.icon} />
                </span>
                <h3 id={`${block.id}-titulo`} className={styles.title}>
                  {block.title}
                  <StatusLabel status={blockStatus} />
                </h3>
                <p className={styles.description}>{block.description}</p>
                <ItemList items={block.items} />
              </div>
              <div className={phone ? styles.phone : undefined}>
                <ProductImage image={block.image} device={phone ? "phone" : "desktop"} />
              </div>
            </article>
          );
        })}
      </div>
    </Section>
  );
}
