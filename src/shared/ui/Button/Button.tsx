import React from 'react';
import { clsx } from 'clsx';

import styles from './Button.module.css';

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'outline' | 'ghost';
  size?: 'sm' | 'md' | 'lg';
  isLoading?: boolean;
}

export const Button: React.FC<ButtonProps> = ({
  className,
  variant = 'primary',
  size = 'md',
  isLoading,
  children,
  ...props
}) => {
  return (
    <button
      className={clsx(
        styles.button,
        styles[variant],
        styles[size],
        isLoading && styles.loading,
        className,
      )}
      disabled={isLoading || props.disabled}
      {...props}
    >
      {isLoading ? <span className={styles.spinner} /> : children}
    </button>
  );
};
