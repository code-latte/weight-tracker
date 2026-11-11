import type { Item } from "@/content/landing";
import Icon from "./icon";
import StatusLabel from "./status-label";
import styles from "./item-list.module.css";

/** A list of capabilities, each with a check and, when it is not available yet, its "Próximamente" label. */
export default function ItemList({ items, onInk = false }: { items: Item[]; onInk?: boolean }) {
  return (
    <ul className={`${styles.list} ${onInk ? styles.onInk : ""}`}>
      {items.map((item) => (
        <li
          key={item.text}
          className={`${styles.item} ${item.status === "soon" ? styles.soon : ""}`}
        >
          <span className={styles.mark}>
            <Icon name="check" size={18} />
          </span>
          <span>
            {item.text}
            <StatusLabel status={item.status} onInk={onInk} />
          </span>
        </li>
      ))}
    </ul>
  );
}
