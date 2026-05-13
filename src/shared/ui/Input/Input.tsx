import React, { forwardRef } from 'react';
import { clsx } from 'clsx';
import styles from './Input.module.css';

interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  leftIcon?: React.ReactNode;
}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ className, label, error, leftIcon, ...props }, ref) => {
    return (
      <div className={clsx(styles.wrapper, className)}>
        {label && <label className={styles.label}>{label}</label>}
        <div className={styles.inputContainer}>
          {leftIcon && <span className={styles.leftIcon}>{leftIcon}</span>}
          <input
            ref={ref}
            className={clsx(styles.input, leftIcon && styles.hasLeftIcon, error && styles.hasError)}
            {...props}
          />
        </div>
        {error && <span className={styles.error}>{error}</span>}
      </div>
    );
  },
);

Input.displayName = 'Input';
