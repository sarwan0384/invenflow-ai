import { cn } from '../../lib/utils';

type BadgeProps = {
  children: React.ReactNode;
  tone?: 'default' | 'success' | 'warning' | 'danger' | 'info';
};

export function Badge({ children, tone = 'default' }: BadgeProps) {
  const tones = {
    default: 'border-white/10 bg-white/5 text-slate-200',
    success: 'border-emerald-500/30 bg-emerald-500/10 text-emerald-300',
    warning: 'border-amber-500/30 bg-amber-500/10 text-amber-300',
    danger: 'border-rose-500/30 bg-rose-500/10 text-rose-300',
    info: 'border-violet-500/30 bg-violet-500/10 text-violet-300',
  };

  return <span className={cn('inline-flex items-center rounded-full border px-2.5 py-1 text-xs font-medium', tones[tone])}>{children}</span>;
}
