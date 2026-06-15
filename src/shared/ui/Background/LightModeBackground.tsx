import React from 'react';
import styles from './LightModeBackground.module.css';

export const LightModeBackground: React.FC = () => {
  return (
    <div className={styles.container} aria-hidden="true">
      {/* Animated Mesh Gradient Blobs */}
      <div className={`${styles.blob} ${styles.blob1}`} />
      <div className={`${styles.blob} ${styles.blob2}`} />
      <div className={`${styles.blob} ${styles.blob3}`} />

      {/* Structured Overlays */}
      <div className={styles.gridOverlay} />
      <div className={styles.scanline} />
    </div>
  );
};
