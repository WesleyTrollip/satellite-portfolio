import { ReactNode } from "react";

type PageHeaderProps = {
  title: string;
  description?: string;
};

export function PageHeader({ title, description }: PageHeaderProps) {
  return (
    <header className="space-y-2">
      <h1>{title}</h1>
      {description ? <p className="muted">{description}</p> : null}
    </header>
  );
}

type CardSectionProps = {
  title?: string;
  description?: string;
  children: ReactNode;
};

export function CardSection({ title, description, children }: CardSectionProps) {
  return (
    <section className="card space-y-4">
      {title ? (
        <header className="space-y-1">
          <h2>{title}</h2>
          {description ? <p className="muted">{description}</p> : null}
        </header>
      ) : null}
      {children}
    </section>
  );
}

export function StatusMessage({ message }: { message: string }) {
  if (!message) {
    return null;
  }

  return (
    <p role="status" aria-live="polite" className="status">
      {message}
    </p>
  );
}

export function EmptyState({ message }: { message: string }) {
  return <p className="muted">{message}</p>;
}
