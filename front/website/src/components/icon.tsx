import {
  Check,
  ChartLine,
  ClipboardList,
  Clock,
  Dumbbell,
  FileStack,
  MessageSquareText,
  MessagesSquare,
  Smartphone,
  Sparkles,
  TrendingUp,
  Users,
  type LucideIcon,
} from "lucide-react";
import type { IconName } from "@/content/landing";

const icons: Record<IconName, LucideIcon> = {
  files: FileStack,
  message: MessageSquareText,
  trending: TrendingUp,
  clock: Clock,
  users: Users,
  clipboard: ClipboardList,
  chart: ChartLine,
  feedback: MessagesSquare,
  smartphone: Smartphone,
  sparkles: Sparkles,
  dumbbell: Dumbbell,
  check: Check,
};

/** An icon the content names, decorative: the text beside it says what it means. */
export default function Icon({ name, size = 24 }: { name: IconName; size?: number }) {
  const Drawing = icons[name];
  return <Drawing size={size} strokeWidth={1.75} aria-hidden="true" focusable="false" />;
}
