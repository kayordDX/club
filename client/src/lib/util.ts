export const getCookie = (name: string) => {
	const nameEQ = name + "=";
	const ca = document.cookie.split(";");
	for (let i = 0; i < ca.length; i++) {
		let c = ca[i];
		while (c.charAt(0) === " ") {
			c = c.substring(1, c.length);
		}
		if (c.indexOf(nameEQ) === 0) {
			return decodeURIComponent(c.substring(nameEQ.length, c.length));
		}
	}
	return null;
};

export const getInitials = (string: string) => {
	const names = string.split(" ");
	let initials = names[0]?.substring(0, 1).toUpperCase();

	if (names.length > 1) {
		initials += names[names.length - 1]?.substring(0, 1)?.toUpperCase() ?? "";
	}
	return initials;
};

export const delay = (ms: number): Promise<void> => {
	return new Promise((resolve) => setTimeout(resolve, ms));
};
