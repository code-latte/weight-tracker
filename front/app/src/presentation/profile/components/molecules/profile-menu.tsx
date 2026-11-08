import Card from "@/presentation/common/components/atoms/card";
import ListItem from "@/presentation/common/components/atoms/list-item";

export interface ProfileMenuItem {
  icon: string;
  title: string;
  description?: string;
}

interface ProfileMenuProps {
  items: ProfileMenuItem[];
}

/** The account menu: one list item per entry, in the given order. The entries have no action yet. */
export default function ProfileMenu({ items }: ProfileMenuProps) {
  return (
    <Card mode="outlined">
      {items.map((item) => (
        <ListItem
          key={item.title}
          icon={item.icon}
          title={item.title}
          description={item.description}
        />
      ))}
    </Card>
  );
}
