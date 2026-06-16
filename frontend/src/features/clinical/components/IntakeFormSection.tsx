interface IntakeFormSectionProps {
  title: string;
  children: React.ReactNode;
}

export function IntakeFormSection({ title, children }: IntakeFormSectionProps) {
  return (
    <section className="bg-card border border-border rounded-lg p-6 shadow-(--shadow-1) mb-6">
      <h2 className="text-lg font-semibold mb-5 pb-3 border-b border-muted">
        {title}
      </h2>
      {children}
    </section>
  );
}
