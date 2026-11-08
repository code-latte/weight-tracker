import { StyleSheet, View } from "react-native";
import Avatar from "@/presentation/common/components/atoms/avatar";
import Card from "@/presentation/common/components/atoms/card";
import ListItem from "@/presentation/common/components/atoms/list-item";
import initials from "@/presentation/common/format/initials";

interface TrainerCardProps {
  title: string;
  name: string;
  surname: string;
}

/** The athlete's current trainer: initials avatar, a title and the trainer's full name. */
export default function TrainerCard({ title, name, surname }: TrainerCardProps) {
  return (
    <Card mode="outlined">
      <ListItem
        title={title}
        description={`${name} ${surname}`}
        left={() => (
          <View style={styles.avatar}>
            <Avatar label={initials(name, surname)} size={40} />
          </View>
        )}
      />
    </Card>
  );
}

const styles = StyleSheet.create({
  avatar: {
    justifyContent: "center",
    paddingLeft: 16,
  },
});
