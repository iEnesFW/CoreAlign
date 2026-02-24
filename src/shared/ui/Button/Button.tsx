import React from 'react';
import { clsx } from 'clsx';
import { twMerge } from 'tailwind-merge'; // Even if Vanilla CSS, we might use utils for class merging if we had tailwind, but here I'll use simple styles mostly. 
// Wait, I installed clsx and tailwind-merge. I can use modules or just scoped classes. 
// Code below uses simple CSS classes defined in a module or global css. 
// Since I want Vanilla CSS, I should probably use CSS Modules for components.

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
                className
            )}
            disabled={isLoading || props.disabled}
            {...props}
        >
            {isLoading ? <span className={styles.spinner} /> : children}
        </button>
    );
};
