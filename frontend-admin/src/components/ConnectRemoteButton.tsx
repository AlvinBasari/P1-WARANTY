import React from 'react';
import { Play, Copy } from 'lucide-react';
import { toast } from 'sonner';

interface ConnectRemoteButtonProps {
  sessionId: string;
  requestId?: number;
  className?: string;
}

export const ConnectRemoteButton: React.FC<ConnectRemoteButtonProps> = ({
  sessionId,
  className = '',
}) => {
  const handleLaunch = () => {
    const protocolUrl = `yourapp://connect?id=${sessionId}`;
    window.location.href = protocolUrl;
    toast.info(`Membuka sesi remote RustDesk ID: ${sessionId}`, {
      description: 'Menjalankan subprocess launcher teknisi.',
    });
  };

  const handleCopy = async (e: React.MouseEvent) => {
    e.stopPropagation();
    try {
      if (navigator.clipboard && window.isSecureContext) {
        await navigator.clipboard.writeText(sessionId);
      } else {
        const textArea = document.createElement('textarea');
        textArea.value = sessionId;
        textArea.style.position = 'fixed';
        textArea.style.left = '-999999px';
        textArea.style.top = '-999999px';
        document.body.appendChild(textArea);
        textArea.focus();
        textArea.select();
        document.execCommand('copy');
        textArea.remove();
      }
      toast.success(`Session ID ${sessionId} disalin ke clipboard`);
    } catch {
      toast.error('Gagal menyalin Session ID');
    }
  };

  return (
    <div className={`inline-flex items-center gap-1 ${className}`}>
      <button
        onClick={handleLaunch}
        className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-btn bg-signal hover:bg-signal-hover text-white text-xs font-semibold shadow-card transition-colors"
        title="Klik 1-kali untuk meluncurkan koneksi remote via RustDesk"
      >
        <Play className="w-3.5 h-3.5 fill-current" />
        <span>Koneksikan Remote</span>
      </button>

      <button
        onClick={handleCopy}
        className="p-1.5 rounded-btn bg-mist-light hover:bg-mist text-ink-muted text-xs transition-colors"
        title="Salin Session ID"
      >
        <Copy className="w-3 h-3" strokeWidth={1.5} />
      </button>
    </div>
  );
};
