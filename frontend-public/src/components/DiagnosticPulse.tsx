import React from 'react';

interface DiagnosticPulseProps {
  status?: 'active' | 'pending' | 'error' | 'idle';
  className?: string;
}

export const DiagnosticPulse: React.FC<DiagnosticPulseProps> = ({ status = 'active', className = '' }) => {
  const getStrokeColor = () => {
    switch (status) {
      case 'active':
        return '#0E7C7B';
      case 'pending':
        return '#C9821F';
      case 'error':
        return '#C24141';
      case 'idle':
      default:
        return '#D8DCE2';
    }
  };

  const getPath = () => {
    switch (status) {
      case 'active':
        return "M 0 12 L 14 12 L 18 4 L 24 20 L 28 12 L 40 12 L 44 8 L 48 16 L 52 12 L 64 12";
      case 'pending':
        return "M 0 12 L 10 12 L 14 6 L 18 18 L 22 6 L 26 18 L 30 12 L 64 12";
      case 'error':
        return "M 0 12 L 20 12 L 24 2 L 28 22 L 32 12 L 64 12";
      case 'idle':
      default:
        return "M 0 12 L 64 12";
    }
  };

  return (
    <div className={`inline-flex items-center gap-1.5 ${className}`}>
      <svg 
        className={`w-14 h-5 stroke-[1.75] fill-none transition-all duration-300 ${status === 'active' ? 'animate-pulse-slow' : ''}`} 
        viewBox="0 0 64 24"
      >
        <path 
          d={getPath()} 
          stroke={getStrokeColor()} 
          strokeLinecap="round" 
          strokeLinejoin="round" 
        />
      </svg>
    </div>
  );
};
