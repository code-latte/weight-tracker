import { Fragment } from "react";
import { View } from "react-native";
import { Divider } from "react-native-paper";
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

/** The account menu: one list item per entry, in the given order, separated by dividers. No action yet. */
export default function ProfileMenu({ items }: ProfileMenuProps) {
  return (
    <Card mode="outlined">
      {/* Card clones each direct child with layout props, which a Fragment rejects; one View takes them instead. */}
      <View>
        {items.map((item, index) => (
          <Fragment key={item.title}>
            {index > 0 && <Divider />}
            <ListItem icon={item.icon} title={item.title} description={item.description} chevron />
          </Fragment>
        ))}
      </View>
    </Card>
  );
}
