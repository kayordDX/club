const CURRENCY_FORMAT = new Intl.NumberFormat("en-ZA", {
	style: "currency",
	currency: "ZAR",
	minimumFractionDigits: 2,
});

const DATE_FORMAT = new Intl.DateTimeFormat("en-ZA", { day: "numeric", month: "short", year: "numeric" });

const TIME_FORMAT = new Intl.DateTimeFormat("en-ZA", { hour: "2-digit", minute: "2-digit", hour12: false });

export const formatDateTime = (value?: string | null) => {
	if (!value) return "—";

	return new Date(value).toLocaleString();
};

export const formatDate = (value?: string | null) => {
	if (!value) return "—";

	return DATE_FORMAT.format(new Date(value));
};

export const formatTime = (value?: string | null) => {
	if (!value) return "—";

	return TIME_FORMAT.format(new Date(value));
};

export const formatCurrency = (value?: number | null) => {
	if (value == null) return "—";

	return CURRENCY_FORMAT.format(value);
};
