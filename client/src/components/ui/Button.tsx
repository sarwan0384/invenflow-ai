import type { ButtonHTMLAttributes, ReactNode } from 'react';
import { Loader2 } from 'lucide-react';
import { cn } from '../../lib/utils';

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  children: ReactNode;
  variant?: 'primary' | 'secondary' | 'ghost';
  loading?: boolean;
};

export function Button({
  children,
  variant = 'primary',
  loading = false,
  className,
  disabled,
  ...props
}: ButtonProps) {
  const base = 'inline-flex items-center justify-center rounded-xl px-4 py-2.5 text-sm font-medium transition-all focus:outline-none focus:ring-2 focus:ring-violet-500/50 disabled:cursor-not-allowed disabled:opacity-60';
  const variants = {
    primary: 'bg-violet-600 text-white shadow-lg shadow-violet-600/20 hover:bg-violet-500',
    secondary: 'border border-white/10 bg-white/5 text-slate-100 hover:bg-white/10',
    ghost: 'bg-transparent text-slate-300 hover:bg-white/5 hover:text-white',
  };

  return (
    <button className={cn(base, variants[variant], className)} disabled={disabled || loading} {...props}>
      {loading ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
      {children}
    </button>
  );
}
